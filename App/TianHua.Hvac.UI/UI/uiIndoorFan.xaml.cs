using AcHelper;
using AcHelper.Commands;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.IO.JSON;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.ParameterService;
using TianHua.Hvac.UI.Command;
using TianHua.Hvac.UI.IndoorFanModels;
using TianHua.Hvac.UI.LoadCalculation;
using TianHua.Hvac.UI.UI.IndoorFan;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiIndoorFan.xaml 的交互逻辑
    /// </summary>
    public partial class uiIndoorFan : ThCustomWindow
    {
        static IndoorFanViewModel indoorFanViewModel;
        string defaultPath = "";
        string saveExtensionName = "thdata";
        SerializableHelper serializableHelper;
        uiIndoorFanCheck fanCheck = null;
        public uiIndoorFan()
        {
            InitializeComponent();
            defaultPath = ThCADCommon.IndoorFanDataTablePath();
            this.MutexName = "THSNJ";
            if (null == indoorFanViewModel)
            {
                indoorFanViewModel = new IndoorFanViewModel();
                //加载数据
                ReadFileData(defaultPath, true);
            }
            
            this.DataContext = indoorFanViewModel;
            wCondRadio.GroupName = indoorFanViewModel.RadioGroupId;
            wCondRadio.TabRadioItemAdd += WCondRadio_TabRadioItemAdd;
            wCondRadio.TabRadioItemDeleted += WCondRadio_TabRadioItemDeleted;
            serializableHelper = new SerializableHelper();
        }
        private void WCondRadio_TabRadioItemDeleted(object sender, RoutedEventArgs e)
        {
            //删除工况
            var rBtn = e.OriginalSource as TabRadioItem;
            var delWorkingId = rBtn.Id;
            IndoorFanData delWokring = null;
            foreach (var item in indoorFanViewModel.SelectInfoFanFile.FileFanDatas) 
            {
                if (item.ShowWorkingData.WorkingId == delWorkingId)
                { 
                    delWokring = item;
                    break;
                }
            }
            if (null == delWokring)
                return;
            indoorFanViewModel.SelectInfoFanFile.FileFanDatas.Remove(delWokring);
        }
        private void WCondRadio_TabRadioItemAdd(object sender, RoutedEventArgs e)
        {
            //新增工况
            var tabItem = (TabRadioItem)e.OriginalSource;
            var selectInfoFanFile = indoorFanViewModel.SelectInfoFanFile;
            string sheetName = "";
            switch (indoorFanViewModel.SelectFanType) 
            {
                case EnumFanType.FanCoilUnitTwoControls:
                    sheetName = "两管制风机盘管";
                    break;
                case EnumFanType.FanCoilUnitFourControls:
                    sheetName = "四管制风机盘管";
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    sheetName = "吊顶一体式空调箱";
                    break;
                case EnumFanType.VRFConditioninConduit:
                    sheetName = "VRF室内机(管道机)";
                    break;
                case EnumFanType.VRFConditioninFourSides:
                    sheetName = "VRF室内机(四面出风型)";
                    break;
            }
            sheetName += "-"+ tabItem.Content;
            var addFanData = new IndoorFanData(selectInfoFanFile.Guid, indoorFanViewModel.SelectFanType, sheetName);
            foreach (var item in indoorFanViewModel.FanInfos)
                addFanData.FanAllDatas.Add(item);
            CalcWorkingData(addFanData, tabItem.Content, tabItem.Id);
            indoorFanViewModel.SelectInfoFanFile.FileFanDatas.Add(addFanData);
        }
        private void btnDetailed_Click(object sender, RoutedEventArgs e)
        {
            FanDataShowViewModel newFanDatas = null;
            switch (indoorFanViewModel.SelectFanType) 
            {
                case EnumFanType.FanCoilUnitFourControls:
                case EnumFanType.FanCoilUnitTwoControls:
                    var fanCoilParameter = new uiFanCoilParameter(
                        indoorFanViewModel.FanInfos.ToList(),
                        indoorFanViewModel.SelectFanType,
                        indoorFanViewModel.SelectWorkingCodition.Content);
                    var res = fanCoilParameter.ShowDialog();
                    if (res != true)
                        return;
                    newFanDatas = fanCoilParameter.GetViewModelData();
                    break;
                case EnumFanType.VRFConditioninConduit:
                case EnumFanType.VRFConditioninFourSides:
                    var vrfParameter = new uiVRFFanParameter(
                        indoorFanViewModel.FanInfos.ToList(),
                        indoorFanViewModel.SelectFanType,
                        indoorFanViewModel.SelectWorkingCodition.Content);
                    var vrfRes = vrfParameter.ShowDialog();
                    if (vrfRes != true)
                        return;
                    newFanDatas = vrfParameter.GetViewModelData();
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    var airParameter = new uiAirFanParameter(
                        indoorFanViewModel.FanInfos.ToList(),
                        indoorFanViewModel.SelectFanType,
                        indoorFanViewModel.SelectWorkingCodition.Content);
                    var airRes = airParameter.ShowDialog();
                    if (airRes != true)
                        return;
                    newFanDatas = airParameter.GetViewModelData();
                    break;
            }
            if (newFanDatas == null)
                return;
            var cSelect = indoorFanViewModel.SelectWorkingCodition;
            //更新数据
            foreach (var fanDatas in indoorFanViewModel.SelectInfoFanFile.FileFanDatas) 
            {
                if (fanDatas.ShowWorkingData.WorkingId != cSelect.Id)
                    continue;
                fanDatas.FanAllDatas.Clear();
                foreach (var item in newFanDatas.FanInfos)
                    fanDatas.FanAllDatas.Add(item);
                CalcWorkingData(fanDatas,
                    fanDatas.ShowWorkingData.WorkingCoditionName,
                    fanDatas.ShowWorkingData.WorkingId,
                    false);
            }
            indoorFanViewModel.SelectWorkingCodition = cSelect;
        }
        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FormUtil.DisableForm(gridForm);
                //设置参数，发送命令
                IndoorFanParameter.Instance.LayoutModel = indoorFanViewModel.FanLayoutModel;
                CommandHandlerBase.ExecuteFromCommandLine(false, "THSNJBZ");
                FocusToCAD();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FormUtil.EnableForm(gridForm);
            }
        }
        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            //放置重复打开
            if (null != fanCheck && fanCheck.IsLoaded)
            {
                fanCheck.ShowActivated = true;
                return;
            }
            try
            {
                this.Hide();
                fanCheck = new uiIndoorFanCheck();
                fanCheck.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                fanCheck.Owner = this;
                fanCheck.Closed += ChildWindowClosed;
                fanCheck.Show();
            }
            catch
            {
                this.Show();
            }
        }
        public void ChildWindowClosed(object sender, EventArgs e)
        {
            this.Show();
        }
        private void btnChange_Click(object sender, RoutedEventArgs e)
        {
            //校核修改
            try
            {
                FormUtil.DisableForm(gridForm);
                //设置参数，发送命令
                IndoorFanParameter.Instance.ChangeLayoutModel = indoorFanViewModel.FanLayoutModel;
                CommandHandlerBase.ExecuteFromCommandLine(false, "THSNJJHXG");
                FocusToCAD();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FormUtil.EnableForm(gridForm);
            }
        }
        private void btnMaterialList_Click(object sender, RoutedEventArgs e)
        {

            FocusToCAD();
            var select = new ThHvacIndoorFanService();
            var pline = select.SelectWindowRect();
            if (null == pline)
                return;
            //材料表
            var time = DateTime.Now.ToString("HHmmss");
            var fileName = "室内机风机数据" + time;
            var fileDialog = new SaveFileDialog();
            fileDialog.Title = "选择保存位置";
            fileDialog.Filter = string.Format("风机数据文件(*.{0})|*.{0}", "xlsx");
            fileDialog.OverwritePrompt = true;
            fileDialog.DefaultExt = saveExtensionName;
            fileDialog.FileName = fileName;
            if (fileDialog.ShowDialog() == true)
            {
                string savePath = fileDialog.FileName;
                if (!CheckPathAndDelFile(savePath))
                {
                    string eMsg = string.Format("文件：{0},删除失败，文件被打开占用，或没有在该位置的权限，请关闭后或修改保存位置", savePath);
                    MessageBox.Show(eMsg, "天华-提醒", MessageBoxButton.OK);
                    return;
                }
                //设置参数
                IndoorFanParameter.Instance.ExportModel = new IndoorFanExportModel();
                IndoorFanParameter.Instance.ExportModel.SavePath = savePath;
                IndoorFanParameter.Instance.ExportModel.FanType = indoorFanViewModel.SelectFanType;
                IndoorFanParameter.Instance.ExportModel.TargetFanInfo.AddRange(indoorFanViewModel.FanInfos);
                IndoorFanParameter.Instance.ExportModel.ExportAreas.Clear();
                IndoorFanParameter.Instance.ExportModel.ExportAreas.Add(pline, new List<Autodesk.AutoCAD.DatabaseServices.Polyline>());
                CommandHandlerBase.ExecuteFromCommandLine(false, "THSNJDC");
                FocusToCAD();
            }
        }
        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = "请选择风机数据文件";
            dialog.Filter = string.Format("风机数据文件(*.{0})|*.{0}", saveExtensionName);
            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.FileName;
                var eMsg = ReadSaveFileData(fileName);
            }
        }

        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var time = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = "室内机风机数据" + time;
            var fileDialog = new SaveFileDialog();
            fileDialog.Title = "选择保存位置";
            fileDialog.Filter = string.Format("风机数据文件(*.{0})|*.{0}", saveExtensionName);
            fileDialog.OverwritePrompt = true;
            fileDialog.DefaultExt = saveExtensionName;
            fileDialog.FileName = fileName;
            if (fileDialog.ShowDialog() == true)
            {
                string savePath = fileDialog.FileName;
                SaveCurrentDataToFile(savePath);
            }
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var filePath = indoorFanViewModel.SelectInfoFanFile.FilePath;
            SaveCurrentDataToFile(filePath);
        }
        
        private void SaveCurrentDataToFile(string savePath)
        {
            if (!CheckPathAndDelFile(savePath))
            {
                string eMsg = string.Format("文件：{0},删除失败，文件被打开占用，或没有在该位置的权限，请关闭后或修改保存位置", savePath);
                MessageBox.Show(eMsg, "天华-提醒", MessageBoxButton.OK);
                return;
            }
            var listObj = GetFanSaveModels();
            var strSave = JsonHelper.SerializeObject(listObj);
            var isTrue = serializableHelper.Serializable(strSave, savePath);
            if (!isTrue)
                MessageBox.Show("文件保存失败！");
        }
        private List<IndoorFanSaveModel> GetFanSaveModels() 
        {
            var resModels = new List<IndoorFanSaveModel>();
            foreach (var item in indoorFanViewModel.SelectInfoFanFile.FileFanDatas) 
            {
                var indoorFanSave = new IndoorFanSaveModel();
                indoorFanSave.SheetName = item.SheetName;
                indoorFanSave.StringData = JsonHelper.SerializeObject(item.FanAllDatas);
                resModels.Add(indoorFanSave);
            }
            return resModels;
        }
        private bool CheckPathAndDelFile (string filePath)
        {
            bool clearS = true;
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);//不存在就创建文件夹
            }
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); }
                catch { clearS = false; }
            }
            return clearS;
        }
        private bool CheckFileInHis(string filePath) 
        {
            foreach (var item in indoorFanViewModel.IndoorFanFiles) 
            {
                if (filePath == item.FilePath)
                    return true;
            }
            return false;
        }
        private string ReadFileData(string filePath,bool isDefault=false) 
        {
            var errorMsg = "";
            //防止当前文档正在打开中，解析会报错，将Excel复制到临时位置进行堆区
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return "文件路径为空或文件不存在";
            if (CheckFileInHis(filePath))
                return errorMsg;
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                ExcelHelper excelService = new ExcelHelper();
                var dataSet = excelService.ReadExcelToDataSet(filePath);
                if (null == dataSet)
                    return errorMsg;
                var addFile = new IndoorFanFile(filePath, dataSet, fileName,isDefault);
                if (isDefault)
                    addFile.ShowName = "默认";
                CalcFanData(addFile,isDefault);
                indoorFanViewModel.DefaultFileIds.Add(addFile.Guid);
                indoorFanViewModel.IndoorFanFiles.Add(addFile);
                indoorFanViewModel.SelectInfoFanFile = addFile;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally 
            { }
            return errorMsg;
        }
        private string ReadSaveFileData(string filePath)
        {
            bool isDefault = false;
            var errorMsg = "";
            //防止当前文档正在打开中，解析会报错，将Excel复制到临时位置进行堆区
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return "文件路径为空或文件不存在";
            if (CheckFileInHis(filePath))
                return errorMsg;

            string dirPath = Path.GetDirectoryName(filePath);
            string fileExt = Path.GetExtension(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string tmpFile = string.Format("~{0}{1}", fileName, fileExt);
            string tempPath = Path.Combine(dirPath, tmpFile);
            try
            {
                File.Copy(filePath, tempPath, true);
                var readStr = serializableHelper.Deserialize(tempPath).ToString();
                var objList = JsonHelper.DeserializeJsonToList<IndoorFanSaveModel>(readStr);
                var addFile = new IndoorFanFile(filePath, null, fileName, isDefault);
                foreach (var item in objList) 
                {
                    var tableName = item.SheetName;
                    var workingName = tableName.Substring(tableName.IndexOf("-") + 1);
                    if (tableName.Contains("两管制风机盘管"))
                    {
                        var listFans = JsonHelper.DeserializeJsonToList<CoilUnitFan>(item.StringData);
                        if (listFans.Count < 1)
                            continue;
                        var indoorFan = new IndoorFanData(addFile.Guid, EnumFanType.FanCoilUnitTwoControls, tableName);
                        indoorFan.FanAllDatas.AddRange(listFans);
                        CalcWorkingData(indoorFan, workingName, "", isDefault);
                        addFile.FileFanDatas.Add(indoorFan);
                    }
                    else if (tableName.Contains("四管制风机盘管"))
                    {
                        var listFans = JsonHelper.DeserializeJsonToList<CoilUnitFan>(item.StringData);
                        if (listFans.Count < 1)
                            continue;
                        var indoorFan = new IndoorFanData(addFile.Guid, EnumFanType.FanCoilUnitFourControls, tableName);
                        indoorFan.FanAllDatas.AddRange(listFans);
                        CalcWorkingData(indoorFan, workingName, "", isDefault);
                        addFile.FileFanDatas.Add(indoorFan);
                    }
                    else if (tableName.Contains("VRF"))
                    {
                        var vrfFans = JsonHelper.DeserializeJsonToList<VRFFan>(item.StringData);
                        var fanType = tableName.Contains("管道机")? EnumFanType.VRFConditioninConduit: EnumFanType.VRFConditioninFourSides;
                        if (vrfFans.Count < 1)
                            continue;
                        var indoorFan = new IndoorFanData(addFile.Guid, fanType, tableName);
                        indoorFan.FanAllDatas.AddRange(vrfFans);
                        CalcWorkingData(indoorFan, workingName, "", isDefault);
                        addFile.FileFanDatas.Add(indoorFan);
                    }
                    else if (tableName.Contains("吊顶一体式"))
                    {
                        var fans = JsonHelper.DeserializeJsonToList<AirConditioninFan>(item.StringData);
                        if (fans.Count < 1)
                            continue;
                        var indoorFan = new IndoorFanData(addFile.Guid, EnumFanType.IntegratedAirConditionin, tableName);
                        indoorFan.FanAllDatas.AddRange(fans);
                        CalcWorkingData(indoorFan, workingName, "", isDefault);
                        addFile.FileFanDatas.Add(indoorFan);
                    }
                }
                indoorFanViewModel.DefaultFileIds.Add(addFile.Guid);
                indoorFanViewModel.IndoorFanFiles.Add(addFile);
                indoorFanViewModel.SelectInfoFanFile = addFile;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { }
                }
            }
            return errorMsg;
        }
        private void CalcFanData(IndoorFanFile indoorFanFile,bool isDefault =false)
        {
            var readFanData = new ReadFanData();
            foreach (DataTable item in indoorFanFile.FanDataSet.Tables)
            {
                var tableName = item.TableName;
                var workingName = tableName.Substring(tableName.IndexOf("-")+1);
                if (tableName.Contains("两管制风机盘管"))
                {
                    var listFans = readFanData.GetCoilUnitFanDatas(item, 5);
                    if (listFans.Count < 1)
                        continue;
                    var indoorFan = new IndoorFanData(indoorFanFile.Guid, EnumFanType.FanCoilUnitTwoControls, tableName);
                    indoorFan.FanAllDatas.AddRange(listFans);
                    CalcWorkingData(indoorFan, workingName, "", isDefault);
                    indoorFanFile.FileFanDatas.Add(indoorFan);
                }
                else if (tableName.Contains("四管制风机盘管"))
                {
                    var listFans = readFanData.GetCoilUnitFanDatas(item, 5);
                    if (listFans.Count < 1)
                        continue;
                    var indoorFan = new IndoorFanData(indoorFanFile.Guid, EnumFanType.FanCoilUnitFourControls, tableName);
                    indoorFan.FanAllDatas.AddRange(listFans);
                    CalcWorkingData(indoorFan, workingName, "", isDefault);
                    indoorFanFile.FileFanDatas.Add(indoorFan);
                }
                else if (tableName.Contains("VRF"))
                {
                    var vrfFans = new List<VRFFan>();
                    var fanType = EnumFanType.VRFConditioninConduit;
                    if (tableName.Contains("管道机"))
                        vrfFans = readFanData.GetVRFPipeFanDatas(item, 5);
                    else
                    {
                        fanType = EnumFanType.VRFConditioninFourSides;
                        vrfFans = readFanData.GetVRFFanDatas(item, 5);
                    }

                    if (vrfFans.Count < 1)
                        continue;
                    var indoorFan = new IndoorFanData(indoorFanFile.Guid, fanType, tableName);
                    indoorFan.FanAllDatas.AddRange(vrfFans);
                    CalcWorkingData(indoorFan, workingName, "", isDefault);
                    indoorFanFile.FileFanDatas.Add(indoorFan);
                }
                else if (tableName.Contains("吊顶一体式")) 
                {
                    var fans = readFanData.GetAirConditioninDatas(item, 5);
                    if (fans.Count < 1)
                        continue;
                    var indoorFan = new IndoorFanData(indoorFanFile.Guid,EnumFanType.IntegratedAirConditionin, tableName);
                    indoorFan.FanAllDatas.AddRange(fans);
                    CalcWorkingData(indoorFan, workingName, "", isDefault);
                    indoorFanFile.FileFanDatas.Add(indoorFan);
                }
            }
        }
        private void CalcWorkingData(IndoorFanData indoorFan,string workingName, string workingId="",bool isDefault=false) 
        {
            workingId = string.IsNullOrEmpty(workingId)? Guid.NewGuid().ToString():workingId;
            //计算工况
            var coilUnitFanWorking = new FanWorkingCondition(indoorFan.Uid, workingId);
            coilUnitFanWorking.WorkingCoditionName = workingName;
            switch (indoorFan.FanType) 
            {
                case EnumFanType.FanCoilUnitTwoControls:
                case EnumFanType.FanCoilUnitFourControls:
                    var fisrtFan = indoorFan.FanAllDatas.First() as CoilUnitFan;
                    var coolWorking = new CoilUnitFanWorkingData(workingId);
                    coolWorking.ShowName = "冷却盘管";
                    coolWorking.AirInletDryBall = fisrtFan.CoolAirInletDryBall;
                    coolWorking.AirInletHumidity = fisrtFan.CoolAirInletHumidity;
                    coolWorking.EnterPortWaterTEMP = fisrtFan.CoolEnterPortWaterTEMP;
                    coolWorking.ExitWaterTEMP = fisrtFan.CoolExitWaterTEMP;
                    coilUnitFanWorking.ShowWorkingDatas.Add(coolWorking);
                    var hotWorking = new CoilUnitFanWorkingData(workingId);
                    hotWorking.ShowName = "加热盘管";
                    hotWorking.AirInletDryBall = fisrtFan.HotAirInletDryBall;
                    hotWorking.AirInletHumidity = "-";
                    hotWorking.EnterPortWaterTEMP = fisrtFan.HotEnterPortWaterTEMP;
                    hotWorking.ExitWaterTEMP = fisrtFan.HotExitWaterTEMP;
                    coilUnitFanWorking.ShowWorkingDatas.Add(hotWorking);
                    indoorFan.ShowWorkingData = coilUnitFanWorking;
                    break;
                case EnumFanType.VRFConditioninFourSides:
                case EnumFanType.VRFConditioninConduit:
                    var firstVRFFan = indoorFan.FanAllDatas.First() as VRFFan;
                    var vrfCoolWorking = new VRFFanWorkingData(workingId);
                    vrfCoolWorking.ShowName = "制冷工况";
                    vrfCoolWorking.AirInletDryBall = firstVRFFan.CoolAirInletDryBall;
                    vrfCoolWorking.AirInletWetBall = firstVRFFan.CoolAirInletWetBall;
                    vrfCoolWorking.OutdoorTemperature = firstVRFFan.CoolOutdoorTemperature;
                    coilUnitFanWorking.ShowWorkingDatas.Add(vrfCoolWorking);
                    var vrfHotWorking = new VRFFanWorkingData(workingId);
                    vrfHotWorking.ShowName = "制热工况";
                    vrfHotWorking.AirInletDryBall = firstVRFFan.HotAirInletDryBall;
                    vrfHotWorking.AirInletWetBall = "-";
                    vrfHotWorking.OutdoorTemperature = firstVRFFan.HotOutdoorTemperature;
                    coilUnitFanWorking.ShowWorkingDatas.Add(vrfHotWorking);
                    indoorFan.ShowWorkingData = coilUnitFanWorking;
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    var firstAirFan = indoorFan.FanAllDatas.First() as AirConditioninFan;
                    var airCoolWorking = new AirConditioninWorkingData(workingId);
                    airCoolWorking.ShowName = "制冷工况";
                    airCoolWorking.AirInletDryBall = firstAirFan.CoolAirInletDryBall;
                    airCoolWorking.AirInletWetBall = firstAirFan.CoolAirInletWetBall;
                    airCoolWorking.EnterPortWaterTEMP = firstAirFan.CoolEnterPortWaterTEMP;
                    airCoolWorking.ExitWaterTEMP = firstAirFan.CoolExitWaterTEMP;
                    coilUnitFanWorking.ShowWorkingDatas.Add(airCoolWorking);
                    var airHotWorking = new AirConditioninWorkingData(workingId);
                    airHotWorking.ShowName = "制热工况";
                    airHotWorking.AirInletDryBall = firstAirFan.HotAirInletTEMP;
                    airHotWorking.AirInletWetBall = "-";
                    airHotWorking.EnterPortWaterTEMP = firstAirFan.HotEnterPortWaterTEMP;
                    airHotWorking.ExitWaterTEMP = firstAirFan.HotExitWaterTEMP;
                    coilUnitFanWorking.ShowWorkingDatas.Add(airHotWorking);
                    indoorFan.ShowWorkingData = coilUnitFanWorking;
                    break;
            }
            if (isDefault)
                indoorFanViewModel.DefaultWorkingIds.Add(workingId);
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FormUtil.DisableForm(gridForm);
                //室内机，放置 设置参数，发送命名
                IndoorFanParameter.Instance.PlaceModel = new IndoorFanPlaceModel();
                IndoorFanParameter.Instance.PlaceModel.LayoutModel = indoorFanViewModel.FanLayoutModel;
                IndoorFanParameter.Instance.PlaceModel.TargetFanInfo = indoorFanViewModel.SelectIndoorFan;
                CommandHandlerBase.ExecuteFromCommandLine(false, "THSNJFZ");
                FocusToCAD();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FormUtil.EnableForm(gridForm);
            }
        }
        private void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        //开始修改时单元格内的值
        string preValue = "";
        private void DataGrid_BeginningEdit(object sender, System.Windows.Controls.DataGridBeginningEditEventArgs e)
        {
            //将修改前的值保存起来
            preValue = (e.Column.GetCellContent(e.Row) as TextBlock).Text;
        }

        private void DataGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            string newValue = (e.EditingElement as TextBox).Text;
            //如果修改后的值和修改前的值不一样
            if (preValue != newValue)
            {
                //工况修改，更新相应的数据
                switch (indoorFanViewModel.SelectFanType)
                {
                    case EnumFanType.FanCoilUnitFourControls:
                    case EnumFanType.FanCoilUnitTwoControls:
                        UpDataCoilFanData();
                        break;
                    case EnumFanType.VRFConditioninConduit:
                    case EnumFanType.VRFConditioninFourSides:
                        UpdataVRFFanData();
                        break;
                    case EnumFanType.IntegratedAirConditionin:
                        UpdataAirConditionFanWorking();
                        break;
                }
            }
        }
        void UpdataAirConditionFanWorking() 
        {
            //更新数据
            var coolWorking = HotCoolWorkingData<AirConditioninWorkingData>(out AirConditioninWorkingData hotWorking);
            var workingId = indoorFanViewModel.SelectWorkingCodition.Id;
            //根据工况信息更新数据
            foreach (var fanFile in indoorFanViewModel.IndoorFanFiles)
            {
                if (fanFile.Guid != indoorFanViewModel.SelectInfoFanFile.Guid)
                    continue;
                foreach (var fanData in fanFile.FileFanDatas)
                {
                    if (fanData.ShowWorkingData.WorkingId != workingId)
                        continue;
                    foreach (AirConditioninFan item in fanData.FanAllDatas)
                    {
                        //修改数据
                        item.CoolAirInletDryBall = coolWorking.AirInletDryBall;
                        item.CoolAirInletWetBall = coolWorking.AirInletWetBall;
                        item.CoolEnterPortWaterTEMP = coolWorking.EnterPortWaterTEMP;
                        item.CoolExitWaterTEMP = coolWorking.ExitWaterTEMP;

                        item.HotAirInletTEMP = hotWorking.AirInletDryBall;
                        item.HotEnterPortWaterTEMP = hotWorking.EnterPortWaterTEMP;
                        item.HotExitWaterTEMP = hotWorking.ExitWaterTEMP;
                    }
                    break;
                }
            }
        }
        void UpDataCoilFanData() 
        {
            //风机盘管数据更新，修改了温度、湿度等信息后的修改
            var coolWorking = HotCoolWorkingData<CoilUnitFanWorkingData>(out CoilUnitFanWorkingData hotWorking);
            var workingId = indoorFanViewModel.SelectWorkingCodition.Id;
            //根据工况信息更新数据
            foreach (var fanFile in indoorFanViewModel.IndoorFanFiles)
            {
                if (fanFile.Guid != indoorFanViewModel.SelectInfoFanFile.Guid)
                    continue;
                foreach (var fanData in fanFile.FileFanDatas)
                {
                    if (fanData.ShowWorkingData.WorkingId != workingId)
                        continue;
                    foreach (CoilUnitFan item in fanData.FanAllDatas)
                    {
                        //修改数据
                        item.CoolAirInletDryBall = coolWorking.AirInletDryBall;
                        item.CoolAirInletHumidity = coolWorking.AirInletHumidity;
                        item.CoolEnterPortWaterTEMP = coolWorking.EnterPortWaterTEMP;
                        item.CoolExitWaterTEMP = coolWorking.ExitWaterTEMP;

                        item.HotAirInletDryBall = hotWorking.AirInletDryBall;
                        item.HotEnterPortWaterTEMP = hotWorking.EnterPortWaterTEMP;
                        item.HotExitWaterTEMP = hotWorking.ExitWaterTEMP;
                    }
                    break;
                }
            }
        }
        void UpdataVRFFanData() 
        {
            var coolWorking = HotCoolWorkingData<VRFFanWorkingData>(out VRFFanWorkingData hotWorking);
            var workingId = indoorFanViewModel.SelectWorkingCodition.Id;
            //根据工况信息更新数据
            foreach (var fanFile in indoorFanViewModel.IndoorFanFiles)
            {
                if (fanFile.Guid != indoorFanViewModel.SelectInfoFanFile.Guid)
                    continue;
                foreach (var fanData in fanFile.FileFanDatas)
                {
                    if (fanData.ShowWorkingData.WorkingId != workingId)
                        continue;
                    foreach (VRFFan item in fanData.FanAllDatas)
                    {
                        //修改数据
                        item.CoolAirInletDryBall = coolWorking.AirInletDryBall;
                        item.CoolAirInletWetBall = coolWorking.AirInletWetBall;
                        item.CoolOutdoorTemperature = coolWorking.OutdoorTemperature;

                        item.HotAirInletDryBall = hotWorking.AirInletDryBall;
                        item.HotOutdoorTemperature = hotWorking.OutdoorTemperature;
                    }
                    break;
                }
            }
        }
        T HotCoolWorkingData<T>(out T hotWorking) where T:class
        {
            var tempCool = indoorFanViewModel.FanTypeWorkingCodition[0];
            var tempHot = indoorFanViewModel.FanTypeWorkingCodition[0];
            foreach (var item in indoorFanViewModel.FanTypeWorkingCodition)
            {
                if (item.ShowName.Contains("热"))
                    tempHot = item;
                else
                    tempCool = item;
            }
            T coolWorking = tempCool as T;
            hotWorking = tempHot as T;
            return coolWorking;
        }

        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://thlearning.thape.com.cn/kng/course/package/video/3dc53d1443b04cda822db7046da629ac_2fc02c71ad1643cabdef012066ba762d.html");
        }
    }
}
