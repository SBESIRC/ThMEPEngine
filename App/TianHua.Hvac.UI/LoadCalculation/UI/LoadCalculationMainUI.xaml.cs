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
using ThMEPEngineCore.IO.IOService;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.LoadCalculation.UI
{
    /// <summary>
    /// LoadCalculationMainUI.xaml 的交互逻辑
    /// </summary>
    public partial class LoadCalculationMainUI : ThCustomWindow
    {
        private SerializableHelper serializableHelper;

        static string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "LoadCalculationConfig");
        static string defaultFile = "Config.config";
        static string installUrl = Path.Combine(urlFolder, defaultFile);
        static string configFolderUrl = Path.Combine(new DirectoryInfo((string)AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX")).Parent.Parent.Parent.FullName, "LoadCalculationConfig");

        LoadCalculationViewModel viewModel;
        ConfigDataModel dataModel = null;

        public LoadCalculationMainUI()
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
            GetNODData();
            CreatBtn.Focus();
        }


        private void GetNODData()
        {
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                var dbSourceService = new ModelDataDbSourceService();
                dbSourceService.LoadFHJS(acad.Database);

                chk_Area.IsChecked = dbSourceService.mainUIData.chk_Area;
                chk_ColdL.IsChecked = dbSourceService.mainUIData.chk_ColdL;
                chk_ColdW.IsChecked = dbSourceService.mainUIData.chk_ColdW;
                chk_ColdWP.IsChecked = dbSourceService.mainUIData.chk_ColdWP;
                chk_ColdWP_Com.SelectedIndex = dbSourceService.mainUIData.chk_ColdWP_Index;
                chk_CondensateWP.IsChecked = dbSourceService.mainUIData.chk_CondensateWP;
                chk_HotL.IsChecked = dbSourceService.mainUIData.chk_HotL;
                chk_HotW.IsChecked = dbSourceService.mainUIData.chk_HotW;
                chk_HotWP.IsChecked = dbSourceService.mainUIData.chk_HotWP;
                chk_HotWP_Com.SelectedIndex = dbSourceService.mainUIData.chk_HotWP_Index;
                chk_AirVolume.IsChecked = dbSourceService.mainUIData.chk_AirVolume;
                chk_FumeExhaust.IsChecked = dbSourceService.mainUIData.chk_FumeExhaust;
                chk_FumeSupplementary.IsChecked = dbSourceService.mainUIData.chk_FumeSupplementary;
                chk_AccidentExhaust.IsChecked = dbSourceService.mainUIData.chk_AccidentExhaust;
                chk_NormalAirVolume.IsChecked = dbSourceService.mainUIData.chk_NormalAirVolume;
                chk_NormalFumeSupplementary.IsChecked = dbSourceService.mainUIData.chk_NormalFumeSupplementary;
                if (configFileList.Items.Count > dbSourceService.mainUIData.ChoiseFileIndex)
                    configFileList.SelectedIndex = dbSourceService.mainUIData.ChoiseFileIndex;
            }
        }

        private void ReadConfig(string fileurl)
        {
            {
                //dataModel = new ConfigDataModel();
                //dataModel.Configs.Clear();
                //dataModel.Configs.Add(new DynamicLoadCalculationModelData()
                //{
                //    RoomFunction = "轻餐",
                //    ColdNorm = new NormClass()
                //    {
                //        ByNorm = true,
                //        NormValue = 200,
                //        TotalValue = 0
                //    },
                //    CWaterTemperature = 6,
                //    HotNorm = new NormClass()
                //    {
                //        ByNorm = true,
                //        NormValue = 100,
                //        TotalValue = 0
                //    },
                //    HWaterTemperature = 10,
                //    ReshAir = new ReshAirVolume()
                //    {
                //        ByNorm = true,
                //        PersonnelDensity = 0.4,
                //        ReshAirNormValue = 30,
                //        TotalValue = 0
                //    },
                //    Lampblack = new LampblackClass()
                //    {
                //        ByNorm = true,
                //        Proportion = 0.33,
                //        AirNum = 60,
                //        TotalValue = 0
                //    },
                //    LampblackAir = new NormClass()
                //    {
                //        ByNorm = true,
                //        NormValue = 0.8,
                //        TotalValue = 0,
                //    },
                //    AccidentAir = new LampblackClass()
                //    {
                //        ByNorm = true,
                //        Proportion = 0.33,
                //        AirNum = 12,
                //        TotalValue = 0
                //    },
                //    Exhaust = new UsuallyExhaust()
                //    {
                //        ByNorm = 1,
                //        NormValue = 12,
                //        TotalValue = 0,
                //        BreatheNum = 12,
                //        CapacityType = 1,
                //        TransformerCapacity = 6400,
                //        BoilerCapacity = 2400,
                //        FirewoodCapacity = 3200,
                //        HeatDissipation = 1,
                //        RoomTemperature = 40
                //    },
                //    AirCompensation = new UsuallyAirCompensation()
                //    {
                //        ByNorm = 1,
                //        NormValue = 0.8,
                //        TotalValue = 0,
                //        CapacityType = 1,
                //        BoilerCapacity = 2400,
                //        FirewoodCapacity = 3200,
                //        CombustionAirVolume = 7,
                //    }
                //});
                //dataModel.Configs.Add(new DynamicLoadCalculationModelData()
                //{
                //    RoomFunction = "重餐",
                //    ColdNorm = new NormClass()
                //    {
                //        ByNorm = false,
                //        NormValue = 200,
                //        TotalValue = 0
                //    },
                //    CWaterTemperature = 6,
                //    HotNorm = new NormClass()
                //    {
                //        ByNorm = true,
                //        NormValue = 100,
                //        TotalValue = 0
                //    },
                //    HWaterTemperature = 10,
                //    ReshAir = new ReshAirVolume()
                //    {
                //        ByNorm = true,
                //        PersonnelDensity = 0.4,
                //        ReshAirNormValue = 30,
                //        TotalValue = 0
                //    },
                //    Lampblack = new LampblackClass()
                //    {
                //        ByNorm = true,
                //        Proportion = 0.33,
                //        AirNum = 60,
                //        TotalValue = 0
                //    },
                //    LampblackAir = new NormClass()
                //    {
                //        ByNorm = true,
                //        NormValue = 0.8,
                //        TotalValue = 0,
                //    },
                //    AccidentAir = new LampblackClass()
                //    {
                //        ByNorm = true,
                //        Proportion = 0.33,
                //        AirNum = 12,
                //        TotalValue = 0
                //    },
                //    Exhaust = new UsuallyExhaust()
                //    {
                //        ByNorm = 3,
                //        NormValue = 12,
                //        TotalValue = 0,
                //        BreatheNum = 12,
                //        CapacityType = 1,
                //        TransformerCapacity = 6400,
                //        BoilerCapacity = 2400,
                //        FirewoodCapacity = 3200,
                //        HeatDissipation = 1,
                //        RoomTemperature = 40
                //    },
                //    AirCompensation = new UsuallyAirCompensation()
                //    {
                //        ByNorm = 1,
                //        NormValue = 0.8,
                //        TotalValue = 0,
                //        CapacityType = 1,
                //        BoilerCapacity = 2400,
                //        FirewoodCapacity = 3200,
                //        CombustionAirVolume = 7,
                //    }
                //});
            }
            dataModel = (ConfigDataModel)serializableHelper.Deserialize(fileurl);
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

            var ClonedataModel = new ConfigDataModel();
            //关联viewModel
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Binder = new UBinder();
                formatter.Serialize(objectStream, dataModel);
                objectStream.Seek(0, SeekOrigin.Begin);
                ClonedataModel = (ConfigDataModel)formatter.Deserialize(objectStream);
            }
            viewModel = new LoadCalculationViewModel(ClonedataModel);
            this.DataContext = viewModel;
        }

        /// <summary>
        /// 保存UI的标注内容配置
        /// </summary>
        private void SaveUIAnnotateContent()
        {
            ThLoadCalculationUIService.Instance.Parameter.chk_Area = chk_Area.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_ColdL = chk_ColdL.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_ColdW = chk_ColdW.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_ColdWP = chk_ColdWP.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_ColdWP_Index = chk_ColdWP_Com.SelectedIndex;
            ThLoadCalculationUIService.Instance.Parameter.chk_CondensateWP = chk_CondensateWP.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_HotL = chk_HotL.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_HotW = chk_HotW.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_HotWP = chk_HotWP.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_HotWP_Index = chk_HotWP_Com.SelectedIndex;
            ThLoadCalculationUIService.Instance.Parameter.chk_AirVolume = chk_AirVolume.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_FumeExhaust = chk_FumeExhaust.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_FumeSupplementary = chk_FumeSupplementary.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_AccidentExhaust = chk_AccidentExhaust.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_NormalAirVolume = chk_NormalAirVolume.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.chk_NormalFumeSupplementary = chk_NormalFumeSupplementary.IsChecked.Value;
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
            dlg.FileName = "室内参数配置文件-上海"; // Default file name
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
        /// 冷指标按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColdNormBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as NormClass;

            ColdNormConfig configUI = new ColdNormConfig(data);
            configUI.Title = "冷指标";
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            configUI.Owner = this;
            var ret = configUI.ShowDialog();
            //if (ret == false)
            //{
            //    //用户取消了操作
            //    return;
            //}
        }

        /// <summary>
        /// 热指标按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HotNormBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as NormClass;

            ColdNormConfig configUI = new ColdNormConfig(data);
            configUI.Title = "热指标";
            configUI.Owner = this;
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var ret = configUI.ShowDialog();
        }

        /// <summary>
        /// 新风量按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReshAirBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as ReshAirVolume;

            ReshAirVolumeConfig configUI = new ReshAirVolumeConfig(data);
            configUI.Owner = this;
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var ret = configUI.ShowDialog();
        }

        /// <summary>
        /// 排油烟按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LampblackBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as LampblackClass;

            LampblackConfig configUI = new LampblackConfig(data);
            configUI.Owner = this;
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var ret = configUI.ShowDialog();
        }

        /// <summary>
        /// 油烟补风按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LampblackAirBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as NormClass;

            LampblackAirConfig configUI = new LampblackAirConfig(data);
            configUI.Owner = this;
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var ret = configUI.ShowDialog();
        }

        /// <summary>
        /// 事故排风按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AccidentAirBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as LampblackClass;

            AccidentAirConfig configUI = new AccidentAirConfig(data);
            configUI.Owner = this;
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var ret = configUI.ShowDialog();
        }

        /// <summary>
        /// 平时排风按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExhaustBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as UsuallyExhaust;

            ExhaustConfig configUI = new ExhaustConfig(data);
            configUI.Owner = this;
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var ret = configUI.ShowDialog();
        }

        /// <summary>
        /// 平时补风按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AirCompensationBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            var data = button.Tag as UsuallyAirCompensation;

            AirCompensationConfig configUI = new AirCompensationConfig(data);
            configUI.Owner = this;
            configUI.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var ret = configUI.ShowDialog();
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
            ThLoadCalculationUIService.Instance.Parameter.ModelDataList = ModelDataList;

            CommandHandlerBase.ExecuteFromCommandLine(false, "THSCFH");
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

        private void chk_ColdWP_Checked(object sender, RoutedEventArgs e)
        {
            if (!chk_ColdWP_Com.IsNull())
            {
                chk_ColdWP_Com.IsEnabled = true;
            }
        }

        private void chk_ColdWP_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!chk_ColdWP_Com.IsNull())
            {
                chk_ColdWP_Com.IsEnabled = false;
            }
        }

        private void chk_HotWP_Checked(object sender, RoutedEventArgs e)
        {
            if (!chk_HotWP_Com.IsNull())
            {
                chk_HotWP_Com.IsEnabled = true;
            }
        }

        private void chk_HotWP_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!chk_HotWP_Com.IsNull())
            {
                chk_HotWP_Com.IsEnabled = false;
            }
        }

        private void ThCustomWindow_Closed(object sender, EventArgs e)
        {
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            using (var doclock = AcadApp.DocumentManager.MdiActiveDocument.LockDocument())
            {
                var dbSourceService = new ModelDataDbSourceService();

                dbSourceService.mainUIData.chk_Area = chk_Area.IsChecked.Value;
                dbSourceService.mainUIData.chk_ColdL = chk_ColdL.IsChecked.Value;
                dbSourceService.mainUIData.chk_ColdW = chk_ColdW.IsChecked.Value;
                dbSourceService.mainUIData.chk_ColdWP = chk_ColdWP.IsChecked.Value;
                dbSourceService.mainUIData.chk_ColdWP_Index = chk_ColdWP_Com.SelectedIndex;
                dbSourceService.mainUIData.chk_CondensateWP = chk_CondensateWP.IsChecked.Value;
                dbSourceService.mainUIData.chk_HotL = chk_HotL.IsChecked.Value;
                dbSourceService.mainUIData.chk_HotW = chk_HotW.IsChecked.Value;
                dbSourceService.mainUIData.chk_HotWP = chk_HotWP.IsChecked.Value;
                dbSourceService.mainUIData.chk_HotWP_Index = chk_HotWP_Com.SelectedIndex;
                dbSourceService.mainUIData.chk_AirVolume = chk_AirVolume.IsChecked.Value;
                dbSourceService.mainUIData.chk_FumeExhaust = chk_FumeExhaust.IsChecked.Value;
                dbSourceService.mainUIData.chk_FumeSupplementary = chk_FumeSupplementary.IsChecked.Value;
                dbSourceService.mainUIData.chk_AccidentExhaust = chk_AccidentExhaust.IsChecked.Value;
                dbSourceService.mainUIData.chk_NormalAirVolume = chk_NormalAirVolume.IsChecked.Value;
                dbSourceService.mainUIData.chk_NormalFumeSupplementary = chk_NormalFumeSupplementary.IsChecked.Value;
                dbSourceService.mainUIData.ChoiseFileIndex = configFileList.SelectedIndex;
                dbSourceService.SaveFHJS(acad.Database);
            }
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
            ThLoadCalculationUIService.Instance.Parameter.RoomFunctionName = roomName;

            CommandHandlerBase.ExecuteFromCommandLine(false, "THFJBHCR");
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

        private void VideoBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var web = "http://thlearning.thape.com.cn/kng/course/package/video/3dc53d1443b04cda822db7046da629ac_b8cf86ebad614a1dbe60b25f685aa133.html";
                System.Diagnostics.Process.Start(web);
            }
            catch (Exception ex)
            {
                MessageBox.Show("抱歉，出现未知错误\r\n" + ex.Message);
            }
        }
    }
}
