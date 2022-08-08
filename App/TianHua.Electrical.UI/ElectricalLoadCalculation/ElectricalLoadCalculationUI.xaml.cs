using AcHelper;
using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThCADExtension;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.ElectricalLoadCalculation;
using ThMEPEngineCore.IO.IOService;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI.ElectricalLoadCalculation
{
    /// <summary>
    /// ElectricalLoadCalculationUI.xaml 的交互逻辑
    /// </summary>
    public partial class ElectricalLoadCalculationUI : ThCustomWindow
    {
        private SerializableHelper serializableHelper;

        static string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "ElectricalLoadCalculation");
        static string defaultFile = "ElectricalConfig.config";
        static string installUrl = Path.Combine(urlFolder, defaultFile);
        static string configFolderUrl = Path.Combine(new DirectoryInfo((string)AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX")).Parent.Parent.Parent.FullName, "ElectricalLoadCalculation");

        ElectricalLoadCalculationViewModel viewModel;
        ElectricalConfigDataModel dataModel = null;

        public ElectricalLoadCalculationUI()
        {
            InitializeComponent();

            
            serializableHelper = new SerializableHelper();

            //读取配置信息
            ReadConfig(installUrl);

            //创建默认Url
            CheckDefaultUrl();

            //为UI绑定数据
            UpdateUIData();

            //获取图纸缓存数据
            CreatBtn.Focus();
        }

        private void ReadConfig(string fileurl)
        {
            dataModel = (ElectricalConfigDataModel)serializableHelper.Deserialize(fileurl);
        }

        /// <summary>
        /// 检查并创建默认url
        /// </summary>
        private void CheckDefaultUrl()
        {
            IOOperateService.CreateFolder(configFolderUrl);
        }

        /// <summary>
        /// 更新UI
        /// </summary>
        private void UpdateUIData(string selected="默认")
        {
            string[] files = Directory.GetFiles(configFolderUrl + @"\", "*.config");
            List<string> fileLst = new List<string>();
            fileLst.Add("默认");
            foreach (string file in files)
            {
                var fileArray = file.Split("\\".ToCharArray());
                fileLst.Add(fileArray[fileArray.Length - 1]);
            }
            configFileList.ItemsSource = fileLst;
            configFileList.SelectedItem = selected;

            var ClonedataModel = new ElectricalConfigDataModel();
            //关联viewModel
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Binder = new UBinder();
                formatter.Serialize(objectStream, dataModel);
                objectStream.Seek(0, SeekOrigin.Begin);
                ClonedataModel = (ElectricalConfigDataModel)formatter.Deserialize(objectStream);
            }
            viewModel = new ElectricalLoadCalculationViewModel(ClonedataModel);
            this.DataContext = viewModel;
        }

        /// <summary>
        /// 保存UI的标注内容配置
        /// </summary>
        private void SaveUIAnnotateContent()
        {
            ElectricalLoadCalculationConfig.chk_Area = chk_Area.IsChecked.Value;
            ElectricalLoadCalculationConfig.chk_ElectricalIndicators = chk_ElectricalIndicators.IsChecked.Value;
            ElectricalLoadCalculationConfig.chk_ElectricalLoad = chk_ElectricalLoad.IsChecked.Value;
        }

        private void UpdateConfig()
        {
            dataModel.Configs = new List<DynamicLoadCalculationModelData>();
            foreach (var config in viewModel.DynamicModelData)
            {
                dataModel.Configs.Add(config);
            }
        }

        private bool SaveConfig(string fileurl)
        {
            return serializableHelper.Serializable(dataModel, fileurl);
        }

        private bool CheckDataUpdates()
        {
            if (viewModel.IsNull())
                return true;//初始化时view为空，跳过检查
            if (dataModel.IsNull())
                return false;
            int count = dataModel.Configs.Count;
            if (viewModel.DynamicModelData.Count != count)
                return false;
            for (int i = 0; i < count; i++)
            {
                var datamodel = dataModel.Configs[i];
                var viewmodel = viewModel.DynamicModelData[i];
                if (datamodel.CompareTo(viewmodel) != 0)
                    return false;
            }
            return true;
        }

        #region Click 事件
        private void configList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string FileName = configFileList.SelectedItem.ToString();
            if (FileName == "默认")
            {
                ReadConfig(installUrl);
                SaveBtn.IsEnabled = false;
                ManualIntervention = false;
                IsReverse = false;
            }
            else
            {
                string file = configFolderUrl + "\\" + FileName;
                //读取配置信息
                ReadConfig(file);
                SaveBtn.IsEnabled = true;
                ManualIntervention = false;
                IsReverse = false;
            }
            UpdateUIData(FileName);
        }

        private void SaveAsFileBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //更新配置
            UpdateConfig();
            //选择路径
            string filePathUrl = string.Empty;
            string fileName = string.Empty;

            filePathUrl = installUrl;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "用电负荷计算配置-上海"; // Default file name
            dlg.DefaultExt = ".config"; // Default file extension
            dlg.Filter = "Config (.config)|*.config"; // Filter files by extension
            dlg.InitialDirectory = configFolderUrl;
            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                filePathUrl = dlg.FileName;
                fileName = dlg.SafeFileName;
                if (filePathUrl != dlg.InitialDirectory + "\\" + fileName)
                {
                    MessageBox.Show("配置文件夹应保存在系统固定目录下，请重新保存！");
                    return;
                }
                if(!SaveConfig(filePathUrl))
                {
                    MessageBox.Show("保存失败，请重新保存！");
                    return;
                }
                UpdateUIData(fileName);
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //更新配置
            UpdateConfig();
            string url = string.Empty;
            string FileName = configFileList.SelectedItem.ToString();
            if (FileName == "默认")
            {
                url = installUrl;
            }
            else
            {
                string file = configFolderUrl + "\\" + FileName;
                //读取配置信息
                url = file;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("错误: 无法找到该文件！", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //保存配置
            SaveConfig(url);
            UpdateUIData(url);
        }

        /// <summary>
        /// 复制按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int index = IndoorParameterTable.SelectedIndex;
            if (index > -1)
            {
                RoomFunctionConfig room = new RoomFunctionConfig(viewModel.DynamicModelData[index].RoomFunction);
                room.Owner = this;
                room.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                var ret = room.ShowDialog();
                if (ret == false)
                {
                    //用户取消了操作
                    return;
                }
                if (string.IsNullOrWhiteSpace(room.RoomName))
                {
                    return;
                }
                var newRowData = new DynamicLoadCalculationModelData();
                var choiseData = viewModel.DynamicModelData[index];
                //DeepClone
                using (Stream objectStream = new MemoryStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Binder = new UBinder();
                    formatter.Serialize(objectStream, choiseData);
                    objectStream.Seek(0, SeekOrigin.Begin);
                    newRowData = (DynamicLoadCalculationModelData)formatter.Deserialize(objectStream);
                }
                newRowData.RoomFunction = room.RoomName;
                viewModel.DynamicModelData.Insert(0, newRowData);
            }
        }

        /// <summary>
        /// 删除按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int index = IndoorParameterTable.SelectedIndex;
            if (index > -1)
            {
                if (MessageBoxResult.Yes == MessageBox.Show($"你确定要删除{viewModel.DynamicModelData[index].RoomFunction}数据吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    viewModel.DynamicModelData.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 房间功能超链接 鼠标离开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RoomTag_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = (InputTextBox)sender;
            if (e.ClickCount == 1)
            {
                var timer = new System.Timers.Timer(500);
                timer.AutoReset = false;
                timer.Elapsed += new ElapsedEventHandler((o, ex) => element.Dispatcher.Invoke(new Action(() =>
                {
                    var timer2 = (System.Timers.Timer)element.Tag;
                    timer2.Stop();
                    timer2.Dispose();
                    UIElement_Click(element, e);
                })));
                timer.Start();
                element.Tag = timer;
            }
            if (e.ClickCount > 1)
            {
                var timer = element.Tag as System.Timers.Timer;
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    UIElement_DoubleClick(sender, e);
                }
            }
        }

        /// <summary>
        /// 生成按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreatBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveUIAnnotateContent();
            var ModelDataList = new List<DynamicLoadCalculationModelData>();
            foreach (var config in viewModel.DynamicModelData)
            {
                ModelDataList.Add(config);
            }
            ElectricalLoadCalculationConfig.ModelDataList = ModelDataList;

            CommandHandlerBase.ExecuteFromCommandLine(false, "THYDFHSC");
            FocusToCAD();
            //this.Close();
        }
        #endregion

        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }

        private bool ShowconfigFileList = false;
        private void configFileList_DropDownOpened(object sender, EventArgs e)
        {
            if (!ShowconfigFileList &&  !CheckDataUpdates() && MessageBoxResult.No == MessageBox.Show("检测到用户未保存已更改数据，是否需要保存?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                ShowconfigFileList = true;
                configFileList.IsDropDownOpen = true;
            }
            ShowconfigFileList = false;
        }
        private void UIElement_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            InputTextBox textBlock = (InputTextBox)sender;
            textBlock.Cursor = null;
            textBlock.PreviewMouseLeftButtonDown -= RoomTag_MouseLeftButtonUp;
            textBlock.LostFocus += TextBox_LostFocus;
            textBlock.EnterEvent += InputTextBox_EnterEvent;
            textBlock.IsReadOnly = false;
            textBlock.Focus();
        }

        private void UIElement_Click(InputTextBox textBlock, MouseButtonEventArgs e)
        {
            string roomName = textBlock.Text;
            ElectricalLoadCalculationConfig.RoomFunctionName = roomName;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THCRFJGNBZ");
            FocusToCAD();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            InputTextBox textBlock = (InputTextBox)sender;
            textBlock.Cursor = Cursors.Hand;
            textBlock.PreviewMouseLeftButtonDown += RoomTag_MouseLeftButtonUp;
            textBlock.LostFocus -= TextBox_LostFocus;
            textBlock.EnterEvent -= InputTextBox_EnterEvent;
            textBlock.IsReadOnly = true ;
        }

        private void InputTextBox_EnterEvent(object sender, RoutedEventArgs e)
        {
            InputTextBox textBlock = (InputTextBox)sender;
            textBlock.Cursor = Cursors.Hand;
            textBlock.PreviewMouseLeftButtonDown += RoomTag_MouseLeftButtonUp;
            textBlock.LostFocus -= TextBox_LostFocus;
            textBlock.EnterEvent -= InputTextBox_EnterEvent;
            textBlock.IsReadOnly = true;
        }

        private bool ManualIntervention = false;//人为排序
        private bool IsReverse = false;//是否倒叙
        private void UpBtn_Click(object sender, RoutedEventArgs e)
        {
            int index = IndoorParameterTable.SelectedIndex;
            if (index > 0)
            {
                var choiseData = viewModel.DynamicModelData[index];
                viewModel.DynamicModelData.Move(index, index - 1);
                ManualIntervention = true;
            }
        }

        private void DownBtn_Click(object sender, RoutedEventArgs e)
        {
            int index = IndoorParameterTable.SelectedIndex;
            if (index > -1 && index < viewModel.DynamicModelData.Count -1)
            {
                var choiseData = viewModel.DynamicModelData[index];
                viewModel.DynamicModelData.Move(index, index + 1);
                ManualIntervention = true;
            }
        }

        private void IndoorParameterTable_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var ModelDataList = viewModel.DynamicModelData.OrderBy(o => o.RoomFunction).ToList();
            if(!ManualIntervention && IsReverse)
            {
                ModelDataList.Reverse();
                IsReverse = false;
            }
            else
            {
                IsReverse = true;
            }
            ManualIntervention = false;
            viewModel.DynamicModelData = new System.Collections.ObjectModel.ObservableCollection<DynamicLoadCalculationModelData>(ModelDataList);
            e.Handled = true;
        }

        private void ElectricalNormBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as PowerSpecifications;

            PowerNormConfig configUI = new PowerNormConfig(data);
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            configUI.Owner = this;
            var ret = configUI.ShowDialog();
        }

        private void VideoBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var web = "http://thlearning.thape.com.cn/kng/view/video/e3bbd55fab1147bf9b9518c4bfb093b4.html?m=1&view=1";
                System.Diagnostics.Process.Start(web);
            }
            catch (Exception ex)
            {
                MessageBox.Show("抱歉，出现未知错误\r\n" + ex.Message);
            }
        }
    }
}
