using AcHelper;
using AcHelper.Commands;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;
using ThMEPHVAC.ParameterService;
using TianHua.Hvac.UI.Command;
using TianHua.Hvac.UI.EQPMFanSelect;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// FanEQPMSelection.xaml 的交互逻辑
    /// </summary>
    public partial class FanEQPMSelection : ThCustomWindow
    {
        EQPMFanSelectViewModel fanViewModel;
        EQPMDocument fanDocument;
        string thisDwgId;
        public FanEQPMSelection()
        {
            InitializeComponent();
            this.MutexName = "THFJXX";
            InitViewModel("");
            fanDocument = new EQPMDocument();
        }
        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            var tempModel = EQPMUIServices.Instance.HisFanViewModels.Where(c => c.DWGID == thisDwgId).FirstOrDefault();
            if (null != tempModel)
            {
                tempModel.ShowInThisDwg = false;
            }
            this.Hide();
        }
        #region 对外
        /// <summary>
        /// CAD的ActiveTab切换时调用
        /// </summary>
        public void ChangeActiveDocument() 
        {
            var docName = Active.DocumentName;
            this.Title = string.Format("风机选型 - {0}", docName);
            thisDwgId = Active.Document.UnmanagedObject.ToString();
            InitViewModel(thisDwgId);
        }
        private void InitViewModel(string id) 
        {
            fanViewModel = null;
            if (string.IsNullOrEmpty(id))
                return;
            var tempModel = EQPMUIServices.Instance.HisFanViewModels.Where(c => c.DWGID == id).FirstOrDefault();
            if (null == tempModel)
            {
                fanViewModel = new EQPMFanSelectViewModel();
                RefreshDataFromDocument();
                EQPMUIServices.Instance.HisFanViewModels.Add(new FanSelectHisModel(id,false,fanViewModel));
            }
            else
            {
                fanViewModel = tempModel.DwgViewModel;
            }
            this.DataContext = fanViewModel;
        }
        public void SelectModelSpaceFanBlock(string fanId)
        {
            if (string.IsNullOrEmpty(fanId))
                return;
            var fanModel = fanViewModel.allFanDataMoedels.Where(c => c.fanDataModel.ID == fanId).FirstOrDefault();
            if (fanModel == null)
            {
                //列表中没有找到相应的风机
                MessageBox.Show("在列表中没有找到风机信息，无法定位选中","天华-提醒",MessageBoxButton.OK);
            }
            else
            {
                fanViewModel.FanInfos.Clear();
                if (fanViewModel.ShowType == EnumEQPMShowType.ShowByFanCode)
                {
                    fanViewModel.FanCodeItem = fanViewModel.FanCodeItems.Where(c => c.Name == fanModel.Name).FirstOrDefault();
                }
                else 
                {
                    fanViewModel.ScenarioSelectItem = fanViewModel.ScenarioItems.Where(c => c.Value == (int)fanModel.fanDataModel.Scenario).FirstOrDefault();
                }
                fanViewModel.SelectFanData = fanViewModel.FanInfos.Where(c => c.fanDataModel.ID == fanId).FirstOrDefault();
            }

        }
        public void RefreshCopyData() 
        {
            //复制后。这里都是添加，不进行删除。
            var allFanDatas = GetDocnmentFanData();
            foreach (var item in allFanDatas)
            {
                if (item.IsChildFan)
                    continue;
                var pItem = fanViewModel.allFanDataMoedels.Where(c => c.fanDataModel.ID == item.fanDataModel.ID).FirstOrDefault();
                if (null != pItem)
                    continue;
                //需要加入
                var addPItem = allFanDatas.Where(c => c.fanDataModel.ID == item.fanDataModel.ID).FirstOrDefault();
                var addCItem = allFanDatas.Where(c => c.IsChildFan && c.fanDataModel.PID == item.fanDataModel.ID).FirstOrDefault();
                if (addCItem != null)
                    fanViewModel.allFanDataMoedels.Add(addCItem);
                fanViewModel.allFanDataMoedels.Add(addPItem);
            }
            //重新触发过滤，显示相应的数据
            if (fanViewModel.ShowType == EnumEQPMShowType.ShowByFanCode)
            {
                fanViewModel.FanCodeItem = fanViewModel.FanCodeItem;
            }
            else
            {
                fanViewModel.ScenarioSelectItem = fanViewModel.ScenarioSelectItem;
            }
            RefreshFanModelByChildFan();
        }
        public void RefreshDeleteData(List<string> changeIds) 
        {
            //删除，撤销都会通过这里进入。将变得过的风机从列表中删除。如果从图纸中读取到的还有，就加入。
            var allFanDatas = GetDocnmentFanData();
            var rmIndexs = new List<int>();
            foreach (var id in changeIds) 
            {
                var pItem = fanViewModel.allFanDataMoedels.Where(c => c.fanDataModel.ID == id).FirstOrDefault();
                var cItem = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == id).FirstOrDefault();
                var addPItem = allFanDatas.Where(c => c.fanDataModel.ID == id).FirstOrDefault();
                var addCItem = allFanDatas.Where(c => c.IsChildFan && c.fanDataModel.PID == id).FirstOrDefault();
                //需要修改,删除旧的加入新的，位置和原来一样
                if (null != pItem)
                {
                    if (addPItem != null)
                    { 
                        addPItem.fanDataModel.SortID = pItem.fanDataModel.SortID;
                        addPItem.fanDataModel.SortScenario = pItem.fanDataModel.SortScenario; 
                    }
                    fanViewModel.allFanDataMoedels.Remove(pItem);
                    if (cItem != null)
                        fanViewModel.allFanDataMoedels.Remove(cItem);
                }
                if (null == addPItem)
                    continue;
                //需要加入
                if (addCItem != null)
                    fanViewModel.allFanDataMoedels.Add(addCItem);
                fanViewModel.allFanDataMoedels.Add(addPItem);
            }
            //重新触发过滤，显示相应的数据
            if (fanViewModel.ShowType == EnumEQPMShowType.ShowByFanCode)
            {
                fanViewModel.FanCodeItem = fanViewModel.FanCodeItem;
            }
            else
            {
                fanViewModel.ScenarioSelectItem = fanViewModel.ScenarioSelectItem;
            }
            RefreshFanModelByChildFan();
        }
        #endregion

        #region 表格内部按钮
        private void btnRemark_Click(object sender, RoutedEventArgs e)
        {
            //备注输入
            var btn = sender as Button;
            var model = (FanDataViewModel)btn.DataContext;
            var remarkUI = new UIFanRemark(model.fanDataModel.Remark);
            var res = remarkUI.ShowDialog();
            if (res == true)
            {
                var strMsg = remarkUI.GetInputRemark(out int type);
                if (type == 99)
                {
                    //应用全部
                    foreach (var item in fanViewModel.FanInfos)
                    {
                        item.fanDataModel.Remark = strMsg;
                    }
                }
                else if (type > 0)
                {
                    //应用当前
                    model.fanDataModel.Remark = strMsg;
                }
            }
        }
        private void btnInsertBlock_Click(object sender, RoutedEventArgs e)
        {
            //插入块
            var btn = sender as Button;
            var model = (FanDataViewModel)btn.DataContext;
            RefreshFanModelParameter(model);
            if (model.fanDataModel.ListVentQuan == null || model.fanDataModel.ListVentQuan.Count < 1) 
            {
                MessageBox.Show("当前行数据没有风机编号，无法进行插入操作,请输入风机编号后再进行插入操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (model.IsRepetitions || string.IsNullOrEmpty(model.FanModelCCCF) || model.FanModelCCCF.Contains("无") || model.FanModelCCCF.Contains("未知"))
            {
                MessageBox.Show("当前行，名称重复或者没有风机型号，无法进行插入图块，请修改数据后再进行操作","天华-提醒",MessageBoxButton.OK,MessageBoxImage.Warning);
                return;
            }
            var childItem = GetChildFanViewModel(model.fanDataModel.ID);
            FanSelectTypeParameter.Instance.FanData = model.fanDataModel;
            FanSelectTypeParameter.Instance.ChildFanData = childItem == null ? null : childItem.fanDataModel;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THFJXXCK");
            FocusToCAD();
        }
        private void btnCalcAirVolume_Click(object sender, RoutedEventArgs e)
        {
            //风机风量计算
            var btn = sender as Button;
            var model = (FanDataViewModel)btn.DataContext;
            var cloneModel = ModelCloneUtil.Copy(model.fanDataModel.VolumeCalcModel);
            var uIVolume = new UIFanVolumeCalc(cloneModel);
            uIVolume.Owner = this;
            var ret = uIVolume.ShowDialog();
            if (ret != true)
                return;
            var newModel = uIVolume.GetNewModel();
            model.AirVolume = newModel.AirVolume;
            model.fanDataModel.VolumeCalcModel = newModel;
            RefreshFanModel(model);
        }
        private void btnCalcFanModel_Click(object sender, RoutedEventArgs e)
        {
            //风机型号计算
            var btn = sender as Button;
            var model = (FanDataViewModel)btn.DataContext;
            if (model.FanControlItem == null) 
            {
                MessageBox.Show("没有选择风机频率");
                return;
            }
            var childItem = GetChildFanViewModel(model.fanDataModel.ID);
            FanDataModel childMode = childItem == null ? null : childItem.fanDataModel;
            UIFanModel uIFan = new UIFanModel(model.fanDataModel, childMode, model.BaseModelPicker, model.CanUseFanModelPickers);
            uIFan.Owner = this;
            var ret = uIFan.ShowDialog();
            if (ret == true)
            {
                var pModel = uIFan.GetNewFanModel(out CalcFanModel newChildModel);
                model.fanDataModel.FanModelTypeCalcModel = pModel;
                model.FanModelCCCF = pModel.FanModelCCCF;
                if (childItem != null)
                {
                    childItem.fanDataModel.FanModelTypeCalcModel = newChildModel;
                    childItem.fanDataModel.FanModelCCCF = pModel.FanModelCCCF;
                }
            }
        }
        private void btnCalcTotalResistance_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var model = (FanDataViewModel)btn.DataContext;
            //阻力计算
            var cloneModel = ModelCloneUtil.Copy(model.fanDataModel.DragModel);
            var uIDrag = new UIDragCalc(cloneModel);
            uIDrag.Owner = this;
            var ret = uIDrag.ShowDialog();
            if (ret != true)
                return;
            var newModel = uIDrag.GetNewCalcModel();
            model.WindResis = newModel.WindResis;
            model.fanDataModel.DragModel = newModel;
            RefreshFanModel(model);
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //风机频率选项修改后，如果改为双频，复制一份子风机
            var btn = sender as ComboBox;
            if (btn.SelectedIndex < 0)
                return;
            var model = (FanDataViewModel)btn.DataContext;
            if (model.IsChildFan)
                return;
            bool haveChild = fanViewModel.allFanDataMoedels.Any(c => c.IsChildFan && c.fanDataModel.PID == model.fanDataModel.ID);
            if (model.fanDataModel.Control == EnumFanControl.TwoSpeed)
            {
                if (haveChild)
                    return;
                else
                {
                    var cloneModel = ModelCloneUtil.Copy(model);
                    cloneModel.fanDataModel.ID = System.Guid.NewGuid().ToString();
                    cloneModel.fanDataModel.PID = model.fanDataModel.ID;
                    cloneModel.IsChildFan = true;
                    var allIndex = fanViewModel.allFanDataMoedels.IndexOf(model);
                    fanViewModel.allFanDataMoedels.Insert(allIndex+1,cloneModel);
                    fanViewModel.SetNewFanModelDefaultValue(cloneModel.fanDataModel.Scenario, ref cloneModel);
                    var index = fanViewModel.FanInfos.IndexOf(model);
                    fanViewModel.FanInfos.Insert(index+1,cloneModel);
                }
            }
            else if (haveChild)
            {
                //删除子子数据
                var delModel = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == model.fanDataModel.ID).FirstOrDefault();
                fanViewModel.allFanDataMoedels.Remove(delModel);
                fanViewModel.FanInfos.Remove(delModel);
            }
        }
        #endregion


        #region 界面上方按钮
        private void btnAddRowCopy_Click(object sender, RoutedEventArgs e)
        {
            var selectFan = fanViewModel.SelectFanData;
            if (selectFan == null)
            {
                if (fanViewModel.ShowType == EnumEQPMShowType.ShowByFanCode)
                {
                    MessageBox.Show("当前没有选中任何风机，无法进行复制");
                    return;
                }
                //没有选中进行添加一个新数据
                var enumScenario = (EnumScenario)fanViewModel.ScenarioSelectItem.Value;
                fanViewModel.AddNewDeafultFanModel(enumScenario);
            }
            else 
            {
                CopyRowItemFans(selectFan);
            }
        }

        private void btnDelRow_Click(object sender, RoutedEventArgs e)
        {
            if (fanViewModel.SelectFanData == null)
            {
                MessageBox.Show("当前没有选中任何风机，无法进行删除");
                return;
            }
            var fanCount = fanViewModel.FanInfos.Where(c => !c.IsChildFan).Count();
            if (fanCount < 2) 
            {
                MessageBox.Show("最后一个风机数据无法删除");
                return;
            }
            var selectFan = fanViewModel.SelectFanData;
            if (selectFan.IsChildFan)
            {
                var res = MessageBox.Show("当前删除的为子风机，删除时会一起删除,是否继续删除", "删除提醒", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;
                var delModel = fanViewModel.allFanDataMoedels.Where(c => c.fanDataModel.ID == selectFan.fanDataModel.PID).FirstOrDefault();
                fanViewModel.allFanDataMoedels.Remove(delModel);
                fanViewModel.FanInfos.Remove(delModel);
            }
            else
            {
                var delModel = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == selectFan.fanDataModel.ID).FirstOrDefault();
                if (delModel != null)
                {
                    var res = MessageBox.Show("当前删除的为主风机(有子风机)，删除时一起删除,是否继续删除", "删除提醒", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.No)
                        return;
                    fanViewModel.allFanDataMoedels.Remove(delModel);
                    fanViewModel.FanInfos.Remove(delModel);
                }
            }
            fanViewModel.allFanDataMoedels.Remove(selectFan);
            fanViewModel.FanInfos.Remove(selectFan);
            fanViewModel.CheckShowFanNumberIsRepeat();
        }

        private void btnMoveUpRow_Click(object sender, RoutedEventArgs e)
        {
            //上移时要考虑主风机和子风机的整体性质
            if (fanViewModel.SelectFanData == null || fanViewModel.FanInfos.Count<2)
                return;
            var oldSelect = fanViewModel.SelectFanData;
            var pModel = fanViewModel.SelectFanData;
            FanDataViewModel cModel;
            if (pModel.IsChildFan)
            {
                cModel = fanViewModel.SelectFanData;
                pModel = fanViewModel.FanInfos.Where(c => c.fanDataModel.ID == cModel.fanDataModel.PID).FirstOrDefault();
            }
            else
            {
                cModel = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == pModel.fanDataModel.ID).FirstOrDefault();
            }
            var pIndex = fanViewModel.FanInfos.IndexOf(pModel);
            var insertIndex = pIndex;
            if (pIndex < 1)
                return;//在最上方，无需排序
            for (int i = 0; i < pIndex; i++) 
            {
                var item = fanViewModel.FanInfos[i];
                if (item.IsChildFan)
                    continue;
                insertIndex = i;
            }
            fanViewModel.FanInfos.Remove(pModel);
            if(cModel != null)
                fanViewModel.FanInfos.Remove(cModel);
            fanViewModel.FanInfos.Insert(insertIndex, pModel);
            if(cModel !=null)
                fanViewModel.FanInfos.Insert(insertIndex+1, cModel);
            fanViewModel.SelectFanData = oldSelect;
            EQPMFanDataUtils.ChangeFanViewModelOrderIds(fanViewModel.FanInfos.ToList(), fanViewModel.ShowType == EnumEQPMShowType.ShowByScenario);
            fanViewModel.IsOrderUp = null;
        }

        private void btnMoveDownRow_Click(object sender, RoutedEventArgs e)
        {
            if (fanViewModel.SelectFanData == null)
                return;

            var oldSelect = fanViewModel.SelectFanData;
            var pModel = fanViewModel.SelectFanData;
            FanDataViewModel cModel;
            if (pModel.IsChildFan)
            {
                cModel = fanViewModel.SelectFanData;
                pModel = fanViewModel.FanInfos.Where(c => c.fanDataModel.ID == cModel.fanDataModel.PID).FirstOrDefault();
            }
            else
            {
                cModel = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == pModel.fanDataModel.ID).FirstOrDefault();
            }
            var pIndex = fanViewModel.FanInfos.IndexOf(pModel);
            var insertIndex = pIndex;
            for (int i = insertIndex+1; i < fanViewModel.FanInfos.Count; i++)
            {
                var item = fanViewModel.FanInfos[i];
                if (item.IsChildFan)
                    continue;
                insertIndex = i;
                if (i + 1 < fanViewModel.FanInfos.Count) 
                {
                    var next = fanViewModel.FanInfos[i+1];
                    if (next.IsChildFan)
                        insertIndex += 1;
                }
                break;
            }
            if (insertIndex == pIndex)
                return;
            fanViewModel.FanInfos.Remove(pModel);
            if (cModel != null)
            {
                fanViewModel.FanInfos.Remove(cModel);
                insertIndex -= 1;
            }
            fanViewModel.FanInfos.Insert(insertIndex, pModel);
            if (cModel != null)
                fanViewModel.FanInfos.Insert(insertIndex + 1, cModel);
            fanViewModel.SelectFanData = oldSelect;
            EQPMFanDataUtils.ChangeFanViewModelOrderIds(fanViewModel.FanInfos.ToList(), fanViewModel.ShowType == EnumEQPMShowType.ShowByScenario);
            fanViewModel.IsOrderUp = null;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("刷新将读取图纸图块数据到面板，未插入图纸的风机数据将被删除，是否继续刷新？","天华-提醒",MessageBoxButton.YesNo,MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;
            RefreshDataFromDocument();
        }

        private void btnExportFile_Click(object sender, RoutedEventArgs e)
        {
            var selectTypes = GetExportFanTypes();
            if (selectTypes.Count < 1)
                return;
            var targetFans = GetExportTargetFans(selectTypes); ;
            if (targetFans.Count < 1)
                return;
            var savePath = GetSavePath("风机计算书");
            if (string.IsNullOrEmpty(savePath))
                return;
            try
            {
                var exportExcel = new EQPMExportExcel(savePath);
                var dataTable = exportExcel.GetFanCalcDataTable(targetFans);
                exportExcel.ExportFanCalc(dataTable);
            }
            catch (Exception ex)
            {
                //防止操作文件失败，导致CAD退出
                MessageBox.Show(ex.Message, "天华-错误提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var selectTypes = GetExportFanTypes();
            if (selectTypes.Count < 1)
                return;
            var targetFans = GetExportTargetFans(selectTypes);
            if (targetFans.Count < 1)
                return;
            var targetFanParas = GetListExportFanPara(targetFans);
            if (targetFanParas.Count < 1)
                return;
            var savePath = GetSavePath("风机参数表");
            if (string.IsNullOrEmpty(savePath))
                return;
            try
            {
                var exportExcel = new EQPMExportExcel(savePath);
                exportExcel.ExportFanParameter(targetFanParas);
            }
            catch (Exception ex)
            {
                //防止操作文件失败，导致CAD退出
                MessageBox.Show(ex.Message, "天华-错误提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        

        #region 
        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        void RefreshDataFromDocument() 
        {
            var calcViewModels = GetDocnmentFanData();
            fanViewModel.allFanDataMoedels.Clear();
            fanViewModel.FanInfos.Clear();
            bool isScenario = fanViewModel.ShowType == EnumEQPMShowType.ShowByScenario;
            if (calcViewModels.Count < 1)
            {
                fanViewModel.AddNewDeafultFanModel((EnumScenario)fanViewModel.ScenarioSelectItem.Value);
            }
            else 
            {
                //先插入子风机,因为主风机修改为双频是会自动加入一个子风机，如果有了，就不会添加了，但是后面要调整显示的顺序
                EQPMFanDataUtils.ChangeFanViewModelOrderIds(calcViewModels, isScenario);
                fanViewModel.allFanDataMoedels.AddRange(calcViewModels);
            }
            //重新触发过滤，显示相应的数据
            if (fanViewModel.ShowType == EnumEQPMShowType.ShowByFanCode)
            {
                fanViewModel.FanCodeItem = fanViewModel.FanCodeItem;
            }
            else 
            {
                fanViewModel.ScenarioSelectItem = fanViewModel.ScenarioSelectItem;
            }
            RefreshFanModelByChildFan();
        }
        void RefreshFanModelByChildFan() 
        {
            if (null == fanViewModel.allFanDataMoedels || fanViewModel.allFanDataMoedels.Count < 1)
                return;
            foreach (var item in fanViewModel.allFanDataMoedels)
            {
                if (item.IsChildFan)
                    continue;
                RefreshFanModel(item);
            }
        }
        List<FanDataViewModel> GetDocnmentFanData() 
        {
            var m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            fanDocument.CheckAndUpdataCopyBlock();
            var allFans = fanDocument.DocumentAreaFanToFanModels(null);
            var calcViewModels = FanModelToViewModels(allFans,true);
            m_DocumentLock.Dispose();
            return calcViewModels;
        }
        void CopyRowItemFans(FanDataViewModel selectFan) 
        {
            FanDataViewModel pModel = null;
            FanDataViewModel cModel = null;
            if (selectFan.IsChildFan)
            {
                var res = MessageBox.Show("当前复制的为子风机，复制时会和主风机一起复制，是否继续", "复制提醒", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;
                pModel = fanViewModel.allFanDataMoedels.Where(c => c.fanDataModel.ID == selectFan.fanDataModel.PID).FirstOrDefault();
                cModel = selectFan;
            }
            else
            {
                pModel = selectFan;
                cModel = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == selectFan.fanDataModel.ID).FirstOrDefault();
                if (cModel != null)
                {
                    var res = MessageBox.Show("当前复制的为主风机(有子风机)，复制时会一起复制,是否继续", "复制提醒", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.No)
                        return;
                }
            }
            var addPModel = new FanDataModel(pModel.fanDataModel.Scenario);
            var addPItem = new FanDataViewModel(addPModel);
            addPModel.IsChildFan = false;
            addPModel.InstallFloor = pModel.fanDataModel.InstallFloor;
            addPModel.InstallSpace = pModel.fanDataModel.InstallSpace;
            addPModel.ServiceArea = pModel.fanDataModel.ServiceArea;
            addPModel.VentNum = pModel.fanDataModel.VentNum;
            addPItem.fanDataModel.VolumeCalcModel = pModel.fanDataModel.VolumeCalcModel;
            addPItem.fanDataModel.DragModel = pModel.fanDataModel.DragModel;
            addPItem.FanEnergyItem = fanViewModel.EnergyItems.Where(c => c.Value == pModel.FanEnergyItem.Value).FirstOrDefault();
            addPItem.MotorEnergyItem = fanViewModel.EnergyItems.Where(c => c.Value == pModel.MotorEnergyItem.Value).FirstOrDefault();
            addPItem.FanControlItem = fanViewModel.FanControlItems.Where(c => c.Value == pModel.FanControlItem.Value).FirstOrDefault();
            if (null != pModel.FanMountTypeItem)
                addPItem.FanMountTypeItem = fanViewModel.FanMountTypeItems.Where(c => c.Value == pModel.FanMountTypeItem.Value).FirstOrDefault();
            else
                addPItem.FanMountTypeItem = fanViewModel.FanMountTypeItems.FirstOrDefault();
            if (null != pModel.VibrationModeItem)
                addPItem.VibrationModeItem = fanViewModel.VibrationModeItems.Where(c => c.Value == pModel.VibrationModeItem.Value).FirstOrDefault();
            else
                addPItem.VibrationModeItem = fanViewModel.VibrationModeItems.FirstOrDefault();
            addPItem.FanTypeItem = addPItem.FanTypeItems.Where(c => c.Value == pModel.FanTypeItem.Value).FirstOrDefault();
            addPItem.AirflowDirectionItem = addPItem.AirflowDirectionItems.Where(c => c.Value == pModel.AirflowDirectionItem.Value).FirstOrDefault();
            addPItem.FanModelCCCF = pModel.FanModelCCCF;
            fanViewModel.allFanDataMoedels.Add(addPItem);
            fanViewModel.FanInfos.Add(addPItem);
            if (null != cModel)
            {
                var addCItem = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == addPModel.ID).FirstOrDefault();
                if (null == addCItem)
                {
                    var addCModel = new FanDataModel(pModel.fanDataModel.Scenario);
                    addCItem = new FanDataViewModel(addCModel);
                    addCItem.fanDataModel.PID = addPModel.ID;
                    addCItem.IsChildFan = true;
                }
                addCItem.fanDataModel.VolumeCalcModel = cModel.fanDataModel.VolumeCalcModel;
                addCItem.fanDataModel.DragModel = cModel.fanDataModel.DragModel;
                addCItem.WindResis = cModel.WindResis;
                addCItem.AirVolume = cModel.AirVolume;
                fanViewModel.allFanDataMoedels.Add(addCItem);
                fanViewModel.FanInfos.Add(addCItem);
            }
            addPItem.AirVolume = pModel.AirVolume;
            addPItem.WindResis = pModel.WindResis;
            RefreshFanModel(addPItem);
            fanViewModel.CheckShowFanNumberIsRepeat();
        }
        private FanDataViewModel GetChildFanViewModel(string id)
        {
            if (null == fanViewModel.allFanDataMoedels || fanViewModel.allFanDataMoedels.Count < 1)
                return null;
            var data = fanViewModel.allFanDataMoedels.Where(c => c.IsChildFan && c.fanDataModel.PID == id).FirstOrDefault();
            if (null == data)
                return null;
            return data;
        }
        private void RefreshFanModel(FanDataViewModel fanDataView, bool isRead = false) 
        {
            bool isChild = fanDataView.fanDataModel.IsChildFan;
            
            FanDataViewModel pViewModel =null;
            FanDataViewModel cViewModel =null;
            if (isChild)
            {
                cViewModel = fanDataView;
                pViewModel = fanViewModel.allFanDataMoedels.Where(c => !c.IsChildFan && c.fanDataModel.ID == fanDataView.fanDataModel.PID).FirstOrDefault();
            }
            else 
            {
                pViewModel = fanDataView;
                cViewModel = GetChildFanViewModel(fanDataView.fanDataModel.ID);
            }
            if(pViewModel.SelectModelPicker != null)
                pViewModel.fanDataModel.IsPointSafe = !pViewModel.SelectModelPicker.IsOptimalModel;
            bool haveChild = cViewModel != null;
            if (haveChild) 
            {
                cViewModel.RefreshFanModel(pViewModel.fanDataModel, isRead);
                if(cViewModel.SelectModelPicker != null)
                    cViewModel.fanDataModel.IsPointSafe = !cViewModel.SelectModelPicker.IsOptimalModel;
            }
            pViewModel.RefreshFanModel(null, isRead);
            var controlType = pViewModel.fanDataModel.Control;
            switch (controlType) 
            {
                case EnumFanControl.TwoSpeed:
                    if (haveChild)
                    {
                        FanModelSelectCheck.CheckAxialFanSelectModel(pViewModel.fanDataModel,
                            cViewModel.fanDataModel, 
                            pViewModel.BaseModelPicker,
                            cViewModel.BaseModelPicker,
                            EQPMFanDataService.Instance.FanParametersAxialD);

                    }
                    else 
                    {
                        FanModelSelectCheck.CheckAxialFanSelectModel(pViewModel.fanDataModel,
                            null,
                            pViewModel.BaseModelPicker,
                            null,
                            EQPMFanDataService.Instance.FanParametersAxialD);
                    }
                    break;
                default:
                    var targetParameters = EQPMFanDataService.Instance.GetTargetFanParameters(controlType, pViewModel.fanDataModel.VentStyle);
                    if (haveChild)
                    {
                        
                        FanModelSelectCheck.CheckFugeFanSelectModel(pViewModel.fanDataModel,
                            cViewModel.fanDataModel,
                            pViewModel.BaseModelPicker,
                            cViewModel.BaseModelPicker,
                            targetParameters);

                    }
                    else
                    {
                        FanModelSelectCheck.CheckFugeFanSelectModel(pViewModel.fanDataModel,
                            null,
                            pViewModel.BaseModelPicker,
                            null,
                            targetParameters);
                    }
                    break;
            }
        }

        private void RefreshFanModelParameter(FanDataViewModel fanDataView) 
        {
            bool isChild = fanDataView.fanDataModel.IsChildFan;
            FanDataViewModel pViewModel = null;
            FanDataViewModel cViewModel = null;
            if (isChild)
            {
                cViewModel = fanDataView;
                pViewModel = fanViewModel.FanInfos.Where(c => !c.IsChildFan && c.fanDataModel.ID == fanDataView.fanDataModel.PID).FirstOrDefault();
            }
            else
            {
                pViewModel = fanDataView;
                cViewModel = GetChildFanViewModel(fanDataView.fanDataModel.ID);
            }
            pViewModel.VentNum = pViewModel.VentNum;
            if (null != cViewModel)
            {
                pViewModel.fanDataModel.AirVolumeDescribe = string.Format("{0}/{1}", pViewModel.fanDataModel.AirVolume, cViewModel.fanDataModel.AirVolume);
                pViewModel.fanDataModel.WindResisDescribe = string.Format("{0}/{1}", pViewModel.fanDataModel.WindResis, cViewModel.fanDataModel.WindResis);
            }
            else 
            {
                pViewModel.fanDataModel.AirVolumeDescribe = pViewModel.fanDataModel.AirVolume.ToString();
                pViewModel.fanDataModel.WindResisDescribe = pViewModel.fanDataModel.WindResis.ToString();
            }
            if (pViewModel.fanDataModel.FanModelTypeCalcModel.ValueSource == EnumValueSource.IsCalcValue)
            {
                if (null != cViewModel)
                {
                    pViewModel.fanDataModel.FanModelPowerDescribe = string.Format("{0}/{1}", 
                        pViewModel.fanDataModel.FanModelTypeCalcModel.FanModelMotorPower,
                        cViewModel.fanDataModel.FanModelTypeCalcModel.FanModelMotorPower);
                }
                else
                {
                    pViewModel.fanDataModel.FanModelPowerDescribe = pViewModel.fanDataModel.FanModelTypeCalcModel.FanModelMotorPower;
                }
            }
            else
            {
                if (null != cViewModel)
                {
                    pViewModel.fanDataModel.FanModelPowerDescribe = string.Format("{0}/{1}",
                        pViewModel.fanDataModel.FanModelTypeCalcModel.FanModelInputMotorPower,
                        cViewModel.fanDataModel.FanModelTypeCalcModel.FanModelInputMotorPower);
                }
                else
                {
                    pViewModel.fanDataModel.FanModelPowerDescribe = pViewModel.fanDataModel.FanModelTypeCalcModel.FanModelInputMotorPower;
                }
            }
        }

        List<FanDataViewModel> FanModelToViewModels(List<FanDataModel> targetFanModels,bool isRead =false) 
        {
            var retViewModels = new List<FanDataViewModel>();
            foreach (var item in targetFanModels)
            {
                if (!item.IsChildFan)
                    continue;
                var addItem = new FanDataViewModel(item);
                addItem.AirVolume = item.AirVolume;
                addItem.WindResis = item.WindResis;
                retViewModels.Add(addItem);
            }
            foreach (var item in targetFanModels)
            {
                if (item.IsChildFan)
                    continue;
                var clone = ModelCloneUtil.Copy(item);
                var addItem = new FanDataViewModel(clone);
                addItem.FanEnergyItem = fanViewModel.EnergyItems.Where(c => c.Value == (int)item.VentLev).FirstOrDefault();
                addItem.MotorEnergyItem = fanViewModel.EnergyItems.Where(c => c.Value == (int)item.EleLev).FirstOrDefault();
                addItem.FanMountTypeItem = fanViewModel.FanMountTypeItems.Where(c => c.Value == (int)item.MountType).FirstOrDefault();
                addItem.VibrationModeItem = fanViewModel.VibrationModeItems.Where(c => c.Value == (int)item.VibrationMode).FirstOrDefault();
                addItem.FanControlItem = fanViewModel.FanControlItems.Where(c => c.Value == (int)item.Control).FirstOrDefault();
                addItem.FanTypeItem = addItem.FanTypeItems.Where(c => c.Value == (int)item.VentStyle).FirstOrDefault();
                addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.Where(c => c.Value == (int)item.IntakeForm).FirstOrDefault();
                if (addItem.AirflowDirectionItem == null)
                    addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.FirstOrDefault();
                addItem.AirVolume = item.AirVolume;
                addItem.WindResis = item.WindResis;
                RefreshFanModelParameter(addItem);
                addItem.FanModelCCCF = item.FanModelCCCF;
                RefreshFanModel(addItem, isRead);
                retViewModels.Add(addItem);
            }
            return retViewModels;
        }

        #endregion
        #region 导出相关
        List<EnumScenario> GetExportFanTypes()
        {
            var resList = new List<EnumScenario>();
            var selectType = new UIExportTypeSelect();
            var res = selectType.ShowDialog();
            if (res != true)
                return resList;
            resList = selectType.GetSelectScenarios();
            return resList;
        }
        string GetSavePath(string attr)
        {
            var fileName = string.Format("{0} - {1}", attr, DateTime.Now.ToString("yyyy.MM.dd HH.mm"));
            var fileDialog = new SaveFileDialog();
            fileDialog.Title = "选择保存位置";
            fileDialog.Filter = string.Format("Xlsx Files(*.{0})|*.{0}", "xlsx");
            fileDialog.OverwritePrompt = true;
            fileDialog.FileName = fileName;
            if (fileDialog.ShowDialog() == true)
            {
                return fileDialog.FileName;
            }
            return "";
        }
        List<FanDataModel> GetExportTargetFans(List<EnumScenario> exportTypes) 
        {
            var resList = new List<FanDataModel>();
            FocusToCAD();
            var select = new ThHvacIndoorFanService();
            var pline = select.SelectWindowRect();
            if (null == pline)
                return resList;
            var m_DocumentLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
            fanDocument.CheckAndUpdataCopyBlock();
            var allFans = fanDocument.DocumentAreaFanToFanModels(pline);
            if (allFans.Count < 1)
                return resList;
            var tempViewModels = FanModelToViewModels(allFans);
            foreach (var item in tempViewModels) 
            {
                if (!exportTypes.Any(c => c == item.fanDataModel.Scenario))
                    continue;
                if (item.fanDataModel.DragModel != null)
                    item.fanDataModel.DragModel.RefeshData();
                resList.Add(item.fanDataModel);
            }
            foreach (var item in tempViewModels)
            {
                //continue;
                if (!item.IsChildFan)
                    continue;
                var pModel = tempViewModels.Where(c => !c.IsChildFan && c.fanDataModel.ID == item.fanDataModel.PID).FirstOrDefault();
                if (null == pModel)
                    continue;
                item.fanDataModel.FanSelectionStateMsg = new FanSelectionStateInfo();
                //item.SelectModelPicker = pModel.SelectModelPicker;
                RefreshFanModel(item);
            }
            return resList;
        }
        private List<ExportFanParaModel> GetListExportFanPara(List<FanDataModel> targetFans)
        {
            var retRes = new List<ExportFanParaModel>();
            foreach (var item in targetFans)
            {
                if (string.IsNullOrEmpty(item.FanModelCCCF) || item.FanModelCCCF.Contains("无") || item.FanModelCCCF.Contains("未知"))
                    continue;
                if (item.IsChildFan)
                    continue;

                var exportFanPara = new ExportFanParaModel();
                exportFanPara.ID = item.ID;
                exportFanPara.Scenario = CommonUtil.GetEnumDescription(item.Scenario);
                exportFanPara.ScenarioCode = item.Name;
                exportFanPara.SortScenario = item.SortScenario;
                exportFanPara.SortID = item.SortID;
                exportFanPara.No = string.Format("{0}-{1}-{2}-{3}", item.Name, item.InstallSpace, item.InstallFloor, item.VentNum);
                exportFanPara.Coverage = item.ServiceArea;
                var strVent = CommonUtil.GetEnumDescription(item.VentStyle);
                exportFanPara.FanForm = strVent.Replace("(电机内置)", "").Replace("(电机外置)", "");

                exportFanPara.FanEnergyLevel = CommonUtil.GetEnumDescription(item.VentLev);

                exportFanPara.ElectricalEnergyLevel = CommonUtil.GetEnumDescription(item.EleLev);
                exportFanPara.MotorPower = item.FanModelPowerDescribe;
                exportFanPara.PowerSource = "380-3-50";
                exportFanPara.ElectricalRpm = item.FanModelTypeCalcModel.MotorTempo.ToString();
                exportFanPara.IsDoubleSpeed = CommonUtil.GetEnumDescription(item.Control); ;
                exportFanPara.IsFrequency = item.Control == EnumFanControl.Inverters ? "是" : "否";
                exportFanPara.WS = item.FanModelTypeCalcModel.FanModelPower;
                exportFanPara.IsFirefighting = item.FanPowerType == EnumFanPowerType.FireFightingPower ? "Y" : "N";
                exportFanPara.VibrationMode = CommonUtil.GetEnumDescription(item.VibrationMode);
                exportFanPara.Amount = item.ListVentQuan.Count().ToString();
                exportFanPara.Remark = item.Remark;
                exportFanPara.FanEfficiency = item.FanModelTypeCalcModel.FanInternalEfficiency;
                exportFanPara.StaticPa = ((item.DragModel.DuctResistance + item.DragModel.Damper) * item.DragModel.SelectionFactor).ToString();

                if (item.VentStyle == EnumFanModelType.AxialFlow)
                {
                    var listAxialFanParameters = EQPMFanDataService.Instance.GetAxialFanParameters(item.Control);
                    var fanParameters = listAxialFanParameters.Find(s => s.No == item.FanModelTypeCalcModel.FanModelNum && s.ModelNum == item.FanModelTypeCalcModel.FanModelName);
                    if (fanParameters != null)
                    {
                        exportFanPara.FanRpm = fanParameters.Rpm;
                        exportFanPara.dB = fanParameters.Noise;
                        exportFanPara.Weight = fanParameters.Weight;
                        exportFanPara.Length = fanParameters.Length;
                        exportFanPara.Width = fanParameters.Diameter;
                        exportFanPara.Height = string.Empty;
                        exportFanPara.DriveMode = "直连";
                    }
                }
                else
                {
                    List<FanParameters> listFanParameters = EQPMFanDataService.Instance.GetTargetFanParameters(item.Control, item.VentStyle);
                    var fanParameters = listFanParameters.Find(s => s.No == item.FanModelTypeCalcModel.FanModelNum && s.CCCF_Spec == item.FanModelTypeCalcModel.FanModelName);
                    if (fanParameters != null)
                    {
                        exportFanPara.FanRpm = fanParameters.Rpm;
                        exportFanPara.dB = fanParameters.Noise;
                        exportFanPara.Weight = fanParameters.Weight;
                        exportFanPara.Length = fanParameters.Length;
                        exportFanPara.Width = fanParameters.Weight;
                        exportFanPara.Height = fanParameters.Height;
                        exportFanPara.DriveMode = "皮带";
                    }

                }
                exportFanPara.CalcAirVolume = item.VolumeCalcModel.AirCalcValue.ToString();
                exportFanPara.FanDelivery = item.AirVolumeDescribe;
                exportFanPara.Pa = item.WindResisDescribe;
                if (item.Control == EnumFanControl.TwoSpeed)
                {
                    var cFan = targetFans.Find(s => s.IsChildFan && s.PID == item.ID);
                    if (cFan != null)
                    {
                        exportFanPara.CalcAirVolume += "/" + cFan.VolumeCalcModel.AirCalcValue.ToString();
                        exportFanPara.StaticPa += "/" + ((cFan.DragModel.DuctResistance + cFan.DragModel.Damper) * cFan.DragModel.SelectionFactor).ToString();
                        exportFanPara.WS +="/"+ cFan.FanModelTypeCalcModel.FanModelPower;
                    }
                }
                retRes.Add(exportFanPara);
            }
            return retRes;
        }
        #endregion

        private void orderUp_Click(object sender, RoutedEventArgs e)
        {
            fanViewModel.IsOrderUp = true;
            FanDataOrderByFloorNum(false);
        }

        private void orderDown_Click(object sender, RoutedEventArgs e)
        {
            fanViewModel.IsOrderUp = false;
            FanDataOrderByFloorNum(true);
        }
        private void FanDataOrderByFloorNum(bool isOrder) 
        {
            var oldSelect = fanViewModel.SelectFanData;
            var orderFans = EQPMFanDataUtils.OrderFanViewModels(fanViewModel.FanInfos.ToList(), isOrder);
            fanViewModel.FanInfos.Clear();
            foreach (var item in orderFans)
            {
                fanViewModel.FanInfos.Add(item);
            }
            fanViewModel.SelectFanData = oldSelect;
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header == null)
                return;
            var headName = e.Column.Header.ToString();
            if (string.IsNullOrEmpty(headName))
                return;
            if (headName != "风机编号")
                return;
            fanViewModel.CheckShowFanNumberIsRepeat();
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://thlearning.thape.com.cn/kng/course/package/video/3dc53d1443b04cda822db7046da629ac_62b0056220bf4819afb1e651386f7a8c.html");
        }
    }

}
