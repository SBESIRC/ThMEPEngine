using AcHelper.Commands;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;
using ThCADExtension;
using ThControlLibraryWPF.CustomControl;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Data;

namespace TianHua.Structure.WPF.UI.BeamStructure.MainBeamConnect
{
    /// <summary>
    /// MainBeamConnectUI.xaml 的交互逻辑
    /// </summary>
    public partial class MainBeamConnectUI : ThCustomWindow
    {
        static string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "BeamStructure");
        static string defaultFile = "MainBeamConfig.json";
        static string installUrl = Path.Combine(urlFolder, defaultFile);
        private MainBeamConfigModel dataModel = null;

        public MainBeamConnectUI()
        {
            InitializeComponent();
            Init(installUrl);
        }

        private void Init(string url)
        {
            // Step 1 读取配置文件
            ReadConfig(url);

            // Step 2 显示UI
            DisplayData(dataModel);
        }

        //读取配置文件
        private void ReadConfig(string url)
        {
            try
            {
                if (!url.IsNull())
                {
                    var data = File.ReadAllText(url);
                    dataModel = JsonConvert.DeserializeObject<MainBeamConfigModel>(data);
                    if (!CheckData(dataModel))
                    {
                        throw new Exception("数据错误！");
                    }
                }
                else
                {
                    dataModel = new MainBeamConfigModel()
                    {
                        SplitSelection = MainBeamConfigFromFile.SplitSelection,
                        SplitArea = MainBeamConfigFromFile.SplitArea,
                        OverLengthSelection = MainBeamConfigFromFile.OverLengthSelection,
                        OverLength = MainBeamConfigFromFile.OverLength,
                        RegionSelection = MainBeamConfigFromFile.RegionSelection
                    };
                }
            }
            catch
            {
                dataModel = new MainBeamConfigModel()
                {
                    SplitSelection = MainBeamConfigFromFile.SplitSelection,
                    SplitArea = MainBeamConfigFromFile.SplitArea,
                    OverLengthSelection = MainBeamConfigFromFile.OverLengthSelection,
                    OverLength = MainBeamConfigFromFile.OverLength,
                    RegionSelection = MainBeamConfigFromFile.RegionSelection
                };
            }
        }

        //将数据写入配置文件
        private void SaveConfig(string url)
        {
            var data = JsonConvert.SerializeObject(dataModel, Formatting.Indented);
            File.WriteAllText(url, data);
        }

        //显示UI
        private void DisplayData(MainBeamConfigModel model)
        {
            this.nonSplit.IsChecked = model.SplitSelection == false;
            this.split.IsChecked = model.SplitSelection == true;
            this.txtSplitArea.Text = model.SplitArea.ToString();
            this.txtOverLength.Text = model.OverLength.ToString();
            this.SelectionRectangle.IsChecked = model.RegionSelection == true;
            this.SelectionPolygon.IsChecked = model.RegionSelection == false;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("你确定要恢复默认选项吗？", "天华-提醒", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Init(null);
                SaveConfig(installUrl);
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if(InputData() && CheckData(dataModel))
            {
                MainBeamLayoutConfig.SplitSelection = dataModel.SplitSelection;
                MainBeamLayoutConfig.SplitArea = dataModel.SplitArea * 1000000;
                MainBeamLayoutConfig.OverLengthSelection = dataModel.OverLengthSelection;
                MainBeamLayoutConfig.OverLength = dataModel.OverLength * 1000;
                MainBeamLayoutConfig.RegionSelection = dataModel.RegionSelection;
                SaveConfig(installUrl);
                CommandHandlerBase.ExecuteFromCommandLine(false, "THZLSC");
                this.Close();
            }
            else
            {
                MessageBox.Show("数据错误：请检查数据格式！", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private bool CheckData(MainBeamConfigModel model)
        {
            if(model.SplitArea < 0 || model.OverLength < 0)
            {
                return false;
            }
            return true;
        }

        private bool InputData()
        {
            try 
            {
                MainBeamConfigModel model = new MainBeamConfigModel();
                model.SplitSelection = this.split.IsChecked == true;
                model.OverLengthSelection = this.overlengthCheck.IsChecked == true;
                model.RegionSelection = this.SelectionRectangle.IsChecked == true;
                model.SplitArea = double.Parse(this.txtSplitArea.Text);
                model.OverLength = double.Parse(this.txtOverLength.Text);
                dataModel = model;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void NonSplit_Checked(object sender, RoutedEventArgs e)
        {
            if (panel1.IsNull())
            {
                return;
            }
            panel1.IsEnabled = false;
        }

        private void Split_Checked(object sender, RoutedEventArgs e)
        {
            if (panel1.IsNull())
            {
                return;
            }
            panel1.IsEnabled = true;
        }

        private void OverLength_Checked(object sender, RoutedEventArgs e)
        {
            if (panel2.IsNull())
            {
                return;
            }
            panel2.IsEnabled = true;
        }

        private void OverLength_UnChecked(object sender, RoutedEventArgs e)
        {
            if (panel2.IsNull())
            {
                return;
            }
            panel2.IsEnabled = false;
        }
    }
}
