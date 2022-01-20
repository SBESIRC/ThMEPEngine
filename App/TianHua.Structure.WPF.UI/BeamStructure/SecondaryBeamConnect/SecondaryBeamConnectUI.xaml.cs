using System;
using System.IO;
using System.Windows;
using ThCADExtension;
using Newtonsoft.Json;
using AcHelper.Commands;
using ThControlLibraryWPF.CustomControl;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;

namespace TianHua.Structure.WPF.UI.BeamStructure.SecondaryBeamConnect
{
    /// <summary>
    /// SecondaryBeamConnectUI.xaml 的交互逻辑
    /// </summary>
    public partial class SecondaryBeamConnectUI : ThCustomWindow
    {
        static string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "BeamStructure");
        static string defaultFile = "SecondaryBeamConfig.json";
        static string installUrl = Path.Combine(urlFolder, defaultFile);
        private SecondaryBeamConfigModel dataModel = null;
        public SecondaryBeamConnectUI()
        {
            InitializeComponent();
            Init(installUrl);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void Init(string url)
        {
            //Step 1 读取配置文件
            ReadConfig(url);

            //Step 2 显示UI
            DisplayData(dataModel);
        }
        private void ReadConfig(string url)
        {
            try
            {
                if (!url.IsNull())
                {
                    var data = File.ReadAllText(url);
                    dataModel = JsonConvert.DeserializeObject<SecondaryBeamConfigModel>(data);
                    if (!CheckData(dataModel))
                    {
                        throw new Exception("数据错误!");
                    }
                }
                else
                {
                    dataModel = new SecondaryBeamConfigModel()
                    {
                        FloorSelection = SecondaryBeamConfigFromFile.FloorSelection,
                        Da = SecondaryBeamConfigFromFile.Da,
                        Db = SecondaryBeamConfigFromFile.Db,
                        Dc = SecondaryBeamConfigFromFile.Dc,
                        RegionSelection = SecondaryBeamConfigFromFile.RegionSelection
                    };
                }
            }
            catch
            {
                dataModel = new SecondaryBeamConfigModel()
                {
                    FloorSelection = SecondaryBeamConfigFromFile.FloorSelection,
                    Da = SecondaryBeamConfigFromFile.Da,
                    Db = SecondaryBeamConfigFromFile.Db,
                    Dc = SecondaryBeamConfigFromFile.Dc,
                    RegionSelection = SecondaryBeamConfigFromFile.RegionSelection
                };
            }
        }

        private void SaveConfig(string url)
        {
            var data = JsonConvert.SerializeObject(dataModel, Formatting.Indented);
            File.WriteAllText(url, data);
        }

        private void DisplayData(SecondaryBeamConfigModel model)
        {
            this.BasementRoof.IsChecked = model.FloorSelection == 1;
            this.BasementMidboard.IsChecked = model.FloorSelection == 2;
            this.txtDa.Text = model.Da.ToString();
            this.txtDb.Text = model.Db.ToString();
            this.txtDc.Text = model.Dc.ToString();
            this.SelectionRectangle.IsChecked = model.RegionSelection == 1;
            this.SelectionPolygon.IsChecked = model.RegionSelection == 2;
        }

        private void ResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MessageBox.Show("你确定要恢复默认选项吗？", "天华-提醒", MessageBoxButton.YesNo, MessageBoxImage.Question)==MessageBoxResult.Yes)
            {
                Init(null);
                SaveConfig(installUrl);
            }
        }
        private void ConfirmButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (InputData() && CheckData(dataModel))
            {
                SecondaryBeamLayoutConfig.Da = dataModel.Da * 1000;//m -> mm
                SecondaryBeamLayoutConfig.Db = dataModel.Db * 1000;//m -> mm
                SecondaryBeamLayoutConfig.Dc = dataModel.Dc * 1000;//m -> mm
                SecondaryBeamLayoutConfig.FloorSelection = dataModel.FloorSelection;
                SecondaryBeamLayoutConfig.RegionSelection = dataModel.RegionSelection;
                SaveConfig(installUrl);
                CommandHandlerBase.ExecuteFromCommandLine(false, "THCLSC");
                this.Close();
            }
            else
            {
                MessageBox.Show("数据错误：请检查数据格式！", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private bool CheckData(SecondaryBeamConfigModel model)
        {
            if (model.FloorSelection != 1 && model.FloorSelection != 2)
            {
                return false;
            }
            if (model.RegionSelection != 1 && model.RegionSelection != 2)
            {
                return false;
            }
            if (model.Da <= 0 || model.Db <= 0 || model.Dc <= 0)
            {
                return false;
            }
            return true;
        }

        private bool InputData()
        {
            try
            {
                SecondaryBeamConfigModel model = new SecondaryBeamConfigModel();
                model.FloorSelection = this.BasementRoof.IsChecked == true ? 1 : 2;
                model.RegionSelection = this.SelectionRectangle.IsChecked == true ? 1 : 2;
                model.Da=double.Parse(this.txtDa.Text);
                model.Db=double.Parse(this.txtDb.Text);
                model.Dc=double.Parse(this.txtDc.Text);
                dataModel = model;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void BasementRoof_Checked(object sender, RoutedEventArgs e)
        {
            if(panel1.IsNull() || panel2.IsNull())
            {
                return;
            }
            panel1.IsEnabled = true;
            panel2.IsEnabled = false;
        }

        private void BasementModboard_Checked(object sender, RoutedEventArgs e)
        {
            if (panel1.IsNull() || panel2.IsNull())
            {
                return;
            }
            panel1.IsEnabled = false;
            panel2.IsEnabled = true;
        }
    }
}
