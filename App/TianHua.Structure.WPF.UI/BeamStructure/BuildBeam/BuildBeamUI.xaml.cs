using System;
using System.IO;
using System.Windows;
using ThCADExtension;
using Newtonsoft.Json;
using AcHelper.Commands;
using ThControlLibraryWPF.CustomControl;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;
using System.Windows.Controls;
using ThMEPStructure.GirderConnect.BuildBeam;

namespace TianHua.Structure.WPF.UI.BeamStructure.BuildBeam
{
    /// <summary>
    /// BuildBeamUI.xaml 的交互逻辑
    /// </summary>
    public partial class BuildBeamUI : ThCustomWindow
    {
        static string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "BeamStructure");
        static string defaultFile = "BuildBeamConfig.json";
        static string installUrl = Path.Combine(urlFolder, defaultFile);
        private BuildBeamConfigModel dataModel = null;
        static BuildBeamViewModel viewModel;
        public BuildBeamUI()
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
                    dataModel = JsonConvert.DeserializeObject<BuildBeamConfigModel>(data);
                    if (!CheckData(dataModel))
                    {
                        throw new Exception("数据错误!");
                    }
                }
                else
                {
                    dataModel = new BuildBeamConfigModel()
                    {
                        EstimateSelection = BuildBeamLayoutConfigFromFile.EstimateSelection,
                        FormulaEstimateSelection = BuildBeamLayoutConfigFromFile.FormulaEstimateSelection,
                        TableEstimateSelection = BuildBeamLayoutConfigFromFile.TableEstimateSelection,
                        BeamCheckSelection = BuildBeamLayoutConfigFromFile.BeamCheckSelection,
                        FormulaTop = BuildBeamLayoutConfigFromFile.FormulaTop,
                        FormulaMiddleA = BuildBeamLayoutConfigFromFile.FormulaMiddleA,
                        FormulaMiddleB = BuildBeamLayoutConfigFromFile.FormulaMiddleB,
                        FormulaMiddleSecondary = BuildBeamLayoutConfigFromFile.FormulaMiddleSecondary,
                        TableTop1 = BuildBeamLayoutConfigFromFile.TableTop1,
                        TableTop2 = BuildBeamLayoutConfigFromFile.TableTop2,
                        TableTop3 = BuildBeamLayoutConfigFromFile.TableTop3,
                        TableTop4 = BuildBeamLayoutConfigFromFile.TableTop4,
                        TableTop5 = BuildBeamLayoutConfigFromFile.TableTop5,
                        TableTop6 = BuildBeamLayoutConfigFromFile.TableTop6,
                        TableTop7 = BuildBeamLayoutConfigFromFile.TableTop7,
                        TableMiddleA1 = BuildBeamLayoutConfigFromFile.TableMiddleA1,
                        TableMiddleA2 = BuildBeamLayoutConfigFromFile.TableMiddleA2,
                        TableMiddleA3 = BuildBeamLayoutConfigFromFile.TableMiddleA3,
                        TableMiddleA4 = BuildBeamLayoutConfigFromFile.TableMiddleA4,
                        TableMiddleA5 = BuildBeamLayoutConfigFromFile.TableMiddleA5,
                        TableMiddleA6 = BuildBeamLayoutConfigFromFile.TableMiddleA6,
                        TableMiddleB1 = BuildBeamLayoutConfigFromFile.TableMiddleB1,
                        TableMiddleB2 = BuildBeamLayoutConfigFromFile.TableMiddleB2,
                        TableMiddleB3 = BuildBeamLayoutConfigFromFile.TableMiddleB3,
                        TableMiddleB4 = BuildBeamLayoutConfigFromFile.TableMiddleB4,
                        TableMiddleB5 = BuildBeamLayoutConfigFromFile.TableMiddleB5,
                        TableMiddleB6 = BuildBeamLayoutConfigFromFile.TableMiddleB6,
                        BeamCheck = BuildBeamLayoutConfigFromFile.BeamCheck,
                        RegionSelection = BuildBeamLayoutConfigFromFile.RegionSelection
                    };
                }
            }
            catch
            {
                dataModel = new BuildBeamConfigModel()
                {
                    EstimateSelection = BuildBeamLayoutConfigFromFile.EstimateSelection,
                    FormulaEstimateSelection = BuildBeamLayoutConfigFromFile.FormulaEstimateSelection,
                    TableEstimateSelection = BuildBeamLayoutConfigFromFile.TableEstimateSelection,
                    BeamCheckSelection = BuildBeamLayoutConfigFromFile.BeamCheckSelection,
                    FormulaTop = BuildBeamLayoutConfigFromFile.FormulaTop,
                    FormulaMiddleA = BuildBeamLayoutConfigFromFile.FormulaMiddleA,
                    FormulaMiddleB = BuildBeamLayoutConfigFromFile.FormulaMiddleB,
                    FormulaMiddleSecondary = BuildBeamLayoutConfigFromFile.FormulaMiddleSecondary,
                    TableTop1 = BuildBeamLayoutConfigFromFile.TableTop1,
                    TableTop2 = BuildBeamLayoutConfigFromFile.TableTop2,
                    TableTop3 = BuildBeamLayoutConfigFromFile.TableTop3,
                    TableTop4 = BuildBeamLayoutConfigFromFile.TableTop4,
                    TableTop5 = BuildBeamLayoutConfigFromFile.TableTop5,
                    TableTop6 = BuildBeamLayoutConfigFromFile.TableTop6,
                    TableTop7 = BuildBeamLayoutConfigFromFile.TableTop7,
                    TableMiddleA1 = BuildBeamLayoutConfigFromFile.TableMiddleA1,
                    TableMiddleA2 = BuildBeamLayoutConfigFromFile.TableMiddleA2,
                    TableMiddleA3 = BuildBeamLayoutConfigFromFile.TableMiddleA3,
                    TableMiddleA4 = BuildBeamLayoutConfigFromFile.TableMiddleA4,
                    TableMiddleA5 = BuildBeamLayoutConfigFromFile.TableMiddleA5,
                    TableMiddleA6 = BuildBeamLayoutConfigFromFile.TableMiddleA6,
                    TableMiddleB1 = BuildBeamLayoutConfigFromFile.TableMiddleB1,
                    TableMiddleB2 = BuildBeamLayoutConfigFromFile.TableMiddleB2,
                    TableMiddleB3 = BuildBeamLayoutConfigFromFile.TableMiddleB3,
                    TableMiddleB4 = BuildBeamLayoutConfigFromFile.TableMiddleB4,
                    TableMiddleB5 = BuildBeamLayoutConfigFromFile.TableMiddleB5,
                    TableMiddleB6 = BuildBeamLayoutConfigFromFile.TableMiddleB6,
                    BeamCheck = BuildBeamLayoutConfigFromFile.BeamCheck,
                    RegionSelection = BuildBeamLayoutConfigFromFile.RegionSelection
                };
            }
        }

        private bool SaveConfig(string url)
        {
            try
            {
                var data = JsonConvert.SerializeObject(dataModel, Formatting.Indented);
                File.WriteAllText(url, data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DisplayData(BuildBeamConfigModel model)
        {
            this.FormulaEstimateRadio.IsChecked = model.EstimateSelection == 1;
            this.TableEstimateRadio.IsChecked = model.EstimateSelection == 2;

            this.FormulaTopRadio.IsChecked = model.FormulaEstimateSelection == 1;
            this.FormulaMiddleRadio.IsChecked = model.FormulaEstimateSelection == 2;

            this.TableTopRadio.IsChecked = model.TableEstimateSelection == 1;
            this.TableMiddleRadio.IsChecked = model.TableEstimateSelection == 2;

            this.BeamCheck.IsChecked = model.BeamCheckSelection == 1;

            this.SelectionRectangle.IsChecked = model.RegionSelection == 1;
            this.SelectionPolygon.IsChecked = model.RegionSelection == 2;

            this.BeamCheckTxt.Text = model.BeamCheck.ToString();

            this.FormulaTop_LDividesHTxt.Text = model.FormulaTop.LDividesH.ToString();
            this.FormulaTop_HDividesBTxt.Text = model.FormulaTop.HDividesB.ToString();
            this.FormulaTop_HminTxt.Text = model.FormulaTop.Hmin.ToString();
            this.FormulaTop_BminTxt.Text = model.FormulaTop.Bmin.ToString();

            this.FormulaMiddleA_LDividesHTxt.Text = model.FormulaMiddleA.LDividesH.ToString();
            this.FormulaMiddleA_HDividesBTxt.Text = model.FormulaMiddleA.HDividesB.ToString();
            this.FormulaMiddleA_HminTxt.Text = model.FormulaMiddleA.Hmin.ToString();
            this.FormulaMiddleA_BminTxt.Text = model.FormulaMiddleA.Bmin.ToString();

            this.FormulaMiddleB_LDividesHTxt.Text = model.FormulaMiddleB.LDividesH.ToString();
            this.FormulaMiddleB_HDividesBTxt.Text = model.FormulaMiddleB.HDividesB.ToString();
            this.FormulaMiddleB_HminTxt.Text = model.FormulaMiddleB.Hmin.ToString();
            this.FormulaMiddleB_BminTxt.Text = model.FormulaMiddleB.Bmin.ToString();

            this.FormulaMiddleSecondary_LDividesHTxt.Text = model.FormulaMiddleSecondary.LDividesH.ToString();
            this.FormulaMiddleSecondary_HDividesBTxt.Text = model.FormulaMiddleSecondary.HDividesB.ToString();
            this.FormulaMiddleSecondary_HminTxt.Text = model.FormulaMiddleSecondary.Hmin.ToString();
            this.FormulaMiddleSecondary_BminTxt.Text = model.FormulaMiddleSecondary.Bmin.ToString();

            viewModel = new BuildBeamViewModel(model);
            this.DataContext = viewModel;
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
                BuildBeamLayoutConfig.EstimateSelection = dataModel.EstimateSelection;
                BuildBeamLayoutConfig.FormulaEstimateSelection = dataModel.FormulaEstimateSelection;
                BuildBeamLayoutConfig.TableEstimateSelection = dataModel.TableEstimateSelection;
                BuildBeamLayoutConfig.BeamCheckSelection = dataModel.BeamCheckSelection;
                BuildBeamLayoutConfig.FormulaTop = dataModel.FormulaTop;
                BuildBeamLayoutConfig.FormulaMiddleA = dataModel.FormulaMiddleA;
                BuildBeamLayoutConfig.FormulaMiddleB = dataModel.FormulaMiddleB;
                BuildBeamLayoutConfig.FormulaMiddleSecondary = dataModel.FormulaMiddleSecondary;
                BuildBeamLayoutConfig.TableTop1 = dataModel.TableTop1;
                BuildBeamLayoutConfig.TableTop2 = dataModel.TableTop2;
                BuildBeamLayoutConfig.TableTop3 = dataModel.TableTop3;
                BuildBeamLayoutConfig.TableTop4 = dataModel.TableTop4;
                BuildBeamLayoutConfig.TableTop5 = dataModel.TableTop5;
                BuildBeamLayoutConfig.TableTop6 = dataModel.TableTop6;
                BuildBeamLayoutConfig.TableTop7 = dataModel.TableTop7;

                BuildBeamLayoutConfig.TableMiddleA1 = dataModel.TableMiddleA1;
                BuildBeamLayoutConfig.TableMiddleA2 = dataModel.TableMiddleA2;
                BuildBeamLayoutConfig.TableMiddleA3 = dataModel.TableMiddleA3;
                BuildBeamLayoutConfig.TableMiddleA4 = dataModel.TableMiddleA4;
                BuildBeamLayoutConfig.TableMiddleA5 = dataModel.TableMiddleA5;
                BuildBeamLayoutConfig.TableMiddleA6 = dataModel.TableMiddleA6;

                BuildBeamLayoutConfig.TableMiddleB1 = dataModel.TableMiddleB1;
                BuildBeamLayoutConfig.TableMiddleB2 = dataModel.TableMiddleB2;
                BuildBeamLayoutConfig.TableMiddleB3 = dataModel.TableMiddleB3;
                BuildBeamLayoutConfig.TableMiddleB4 = dataModel.TableMiddleB4;
                BuildBeamLayoutConfig.TableMiddleB5 = dataModel.TableMiddleB5;
                BuildBeamLayoutConfig.TableMiddleB6 = dataModel.TableMiddleB6;

                BuildBeamLayoutConfig.BeamCheck = dataModel.BeamCheck;
                BuildBeamLayoutConfig.RegionSelection = dataModel.RegionSelection;

                SaveConfig(installUrl);
                CommandHandlerBase.ExecuteFromCommandLine(false, "THSXSC");
                this.Close();
            }
            else
            {
                MessageBox.Show("数据错误：请检查数据格式！", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private bool CheckData(BuildBeamConfigModel model)
        {
            if (model.EstimateSelection != 1 && model.EstimateSelection != 2)
            {
                return false;
            }
            if (model.FormulaEstimateSelection != 1 && model.FormulaEstimateSelection != 2)
            {
                return false;
            }
            if (model.TableEstimateSelection != 1 && model.TableEstimateSelection != 2)
            {
                return false;
            }
            if (model.BeamCheckSelection != 1 && model.BeamCheckSelection != 0)
            {
                return false;
            }
            if (model.RegionSelection != 1 && model.RegionSelection != 2)
            {
                return false;
            }
            if(model.BeamCheck < 0)
            {
                return false;
            }
            return true;
        }

        private bool InputData()
        {
            try
            {
                BuildBeamConfigModel model = new BuildBeamConfigModel();
                model.EstimateSelection = this.FormulaEstimateRadio.IsChecked == true ? 1 : 2;
                model.FormulaEstimateSelection = this.FormulaTopRadio.IsChecked== true ? 1 : 2;
                model.TableEstimateSelection = this.TableTopRadio.IsChecked== true ? 1 : 2;
                model.BeamCheckSelection = this.BeamCheck.IsChecked== true ? 1 : 0;
                model.RegionSelection = this.SelectionRectangle.IsChecked== true ? 1 : 2;
                model.BeamCheck = int.Parse(this.BeamCheckTxt.Text);

                model.FormulaTop.LDividesH = int.Parse(this.FormulaTop_LDividesHTxt.Text);
                model.FormulaTop.HDividesB = int.Parse(this.FormulaTop_HDividesBTxt.Text);
                model.FormulaTop.Hmin = int.Parse(this.FormulaTop_HminTxt.Text);
                model.FormulaTop.Bmin = int.Parse(this.FormulaTop_BminTxt.Text);

                model.FormulaMiddleA.LDividesH = int.Parse(this.FormulaMiddleA_LDividesHTxt.Text);
                model.FormulaMiddleA.HDividesB = int.Parse(this.FormulaMiddleA_HDividesBTxt.Text);
                model.FormulaMiddleA.Hmin = int.Parse(this.FormulaMiddleA_HminTxt.Text);
                model.FormulaMiddleA.Bmin = int.Parse(this.FormulaMiddleA_BminTxt.Text);

                model.FormulaMiddleB.LDividesH = int.Parse(this.FormulaMiddleB_LDividesHTxt.Text);
                model.FormulaMiddleB.HDividesB = int.Parse(this.FormulaMiddleB_HDividesBTxt.Text);
                model.FormulaMiddleB.Hmin = int.Parse(this.FormulaMiddleB_HminTxt.Text);
                model.FormulaMiddleB.Bmin = int.Parse(this.FormulaMiddleB_BminTxt.Text);

                model.FormulaMiddleSecondary.LDividesH = int.Parse(this.FormulaMiddleSecondary_LDividesHTxt.Text);
                model.FormulaMiddleSecondary.HDividesB = int.Parse(this.FormulaMiddleSecondary_HDividesBTxt.Text);
                model.FormulaMiddleSecondary.Hmin = int.Parse(this.FormulaMiddleSecondary_HminTxt.Text);
                model.FormulaMiddleSecondary.Bmin = int.Parse(this.FormulaMiddleSecondary_BminTxt.Text);

                model.TableTop1.H =viewModel.TopPlate[0].H;
                model.TableTop1.B =viewModel.TopPlate[0].B;
                model.TableTop2.H =viewModel.TopPlate[1].H;
                model.TableTop2.B =viewModel.TopPlate[1].B;
                model.TableTop3.H =viewModel.TopPlate[2].H;
                model.TableTop3.B =viewModel.TopPlate[2].B;
                model.TableTop4.H =viewModel.TopPlate[3].H;
                model.TableTop4.B =viewModel.TopPlate[3].B;
                model.TableTop5.H =viewModel.TopPlate[4].H;
                model.TableTop5.B =viewModel.TopPlate[4].B;
                model.TableTop6.H =viewModel.TopPlate[5].H;
                model.TableTop6.B =viewModel.TopPlate[5].B;
                model.TableTop7.H =viewModel.TopPlate[6].H;
                model.TableTop7.B =viewModel.TopPlate[6].B;

                model.TableMiddleA1.H =viewModel.MiddlePlateA[0].H;
                model.TableMiddleA1.B =viewModel.MiddlePlateA[0].B;
                model.TableMiddleA2.H =viewModel.MiddlePlateA[1].H;
                model.TableMiddleA2.B =viewModel.MiddlePlateA[1].B;
                model.TableMiddleA3.H =viewModel.MiddlePlateA[2].H;
                model.TableMiddleA3.B =viewModel.MiddlePlateA[2].B;
                model.TableMiddleA4.H =viewModel.MiddlePlateA[3].H;
                model.TableMiddleA4.B =viewModel.MiddlePlateA[3].B;
                model.TableMiddleA5.H =viewModel.MiddlePlateA[4].H;
                model.TableMiddleA5.B =viewModel.MiddlePlateA[4].B;
                model.TableMiddleA6.H =viewModel.MiddlePlateA[5].H;
                model.TableMiddleA6.B =viewModel.MiddlePlateA[5].B;

                model.TableMiddleB1.H =viewModel.MiddlePlateB[0].H;
                model.TableMiddleB1.B =viewModel.MiddlePlateB[0].B;
                model.TableMiddleB2.H =viewModel.MiddlePlateB[1].H;
                model.TableMiddleB2.B =viewModel.MiddlePlateB[1].B;
                model.TableMiddleB3.H =viewModel.MiddlePlateB[2].H;
                model.TableMiddleB3.B =viewModel.MiddlePlateB[2].B;
                model.TableMiddleB4.H =viewModel.MiddlePlateB[3].H;
                model.TableMiddleB4.B =viewModel.MiddlePlateB[3].B;
                model.TableMiddleB5.H =viewModel.MiddlePlateB[4].H;
                model.TableMiddleB5.B =viewModel.MiddlePlateB[4].B;
                model.TableMiddleB6.H =viewModel.MiddlePlateB[5].H;
                model.TableMiddleB6.B =viewModel.MiddlePlateB[5].B;

                dataModel = model;
                return true;
            }
            catch
            {
                return false;
            }
        }


        private void TableEstimateRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (TableEstimatePanel.IsNull() || FormulaEstimatePanel.IsNull())
            {
                return;
            }
            FormulaEstimateRadio.IsChecked = false;
            FormulaEstimatePanel.IsEnabled = false;
            TableEstimatePanel.IsEnabled = true;
        }

        private void FormulaEstimateRadio_Checked(object sender, RoutedEventArgs e)
        {
            if(TableEstimatePanel.IsNull() || FormulaEstimatePanel.IsNull())
            {
                return;
            }
            TableEstimateRadio.IsChecked =false;
            FormulaEstimatePanel.IsEnabled = true;
            TableEstimatePanel.IsEnabled = false;
        }


        private void FormulaTopRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (FormulaTopPanel.IsNull() || FormulaMiddlePanel.IsNull())
            {
                return;
            }
            FormulaTopPanel.IsEnabled = true;
            FormulaMiddlePanel.IsEnabled = false;
        }

        private void FormulaMiddleRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (FormulaTopPanel.IsNull() || FormulaMiddlePanel.IsNull())
            {
                return;
            }
            FormulaTopPanel.IsEnabled = false ;
            FormulaMiddlePanel.IsEnabled = true ;
        }

        private void TableTopRadiol_Checked(object sender, RoutedEventArgs e)
        {
            if (TableTopPanel.IsNull() || TableMiddlePanel.IsNull())
            {
                return;
            }
            TableMiddleRadio.IsChecked =false;
            TableTopPanel.IsEnabled = true;
            TableMiddlePanel.IsEnabled = false;
        }

        private void TableMiddleRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (TableTopPanel.IsNull() || TableMiddlePanel.IsNull())
            {
                return;
            }
            TableTopRadio.IsChecked =false;
            TableTopPanel.IsEnabled = false;
            TableMiddlePanel.IsEnabled = true ;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Json (.json)|*.json"; // Filter files by extension
            dlg.DefaultExt = ".json"; // Default file extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                // Load document
                Init(dlg.FileName);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            //选择路径
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "天华截面配置"; // Default file name
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "Json (.json)|*.json"; // Filter files by extension
            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                if (!SaveConfig(dlg.FileName))
                {
                    MessageBox.Show("保存失败，请重新保存！");
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }
}
