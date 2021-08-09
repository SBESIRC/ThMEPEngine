using AcHelper;
using AcHelper.Commands;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThCADExtension;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.Service;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.IO.IOService;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI.SecurityPlaneUI
{
    /// <summary>
    /// uiEvaIndicatorSign.xaml 的交互逻辑
    /// </summary>
    public partial class SecurityPlaneSystemUI : ThCustomWindow
    {
        static string urlFolder = Path.Combine(ThCADCommon.SupportPath(), "SecurityPlaneConfig");
        static string defaultFile = "上海地区住宅-安防配置表.xlsx";
        static string installUrl = urlFolder + "\\" + defaultFile;
        static string configFolderUrl = (string)AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX") + "\\SecurityPlaneConfig";
        static string configFileUrl = configFolderUrl + "\\" + defaultFile;

        DataSet configSet = null;
        public SecurityPlaneSystemUI()
        {
            InitializeComponent();

            //设置填充listview
            var dataSet = GetExcelContent(installUrl);
            SetListView(dataSet);

            //创建默认Url
            CheckDefaultUrl();

            //设置默认值
            SetDefaultValue();
        }

        /// <summary>
        /// 填充listView
        /// </summary>
        private void SetListView(DataSet dataSet)
        {
            configSet = dataSet;
            var configTable = dataSet.Tables[ThElectricalUIService.Instance.Parameter.Configs];

            foreach (DataTable table in dataSet.Tables)
            {
                if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.VideoMonitoringSystem))
                {
                    ThElectricalUIService.Instance.Parameter.videoMonitoringSystemTable = table;
                    SetGridValue(VideoMonitoringGrid, table, GetConfigCollection(configTable, 0));
                }
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.IntrusionAlarmSystem))
                {
                    ThElectricalUIService.Instance.Parameter.intrusionAlarmSystemTable = table;
                    SetGridValue(IntrusionAlarmGrid, table, GetConfigCollection(configTable, 1));
                }
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.AccessControlSystem))
                {
                    ThElectricalUIService.Instance.Parameter.accessControlSystemTable = table;
                    SetGridValue(AccessControlGrid, table, GetConfigCollection(configTable, 2));
                }
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.GuardTourSystem))
                {
                    ThElectricalUIService.Instance.Parameter.guardTourSystemTable = table;
                    GuardTourGrid.ItemsSource = table.DefaultView;
                    List<string> configLst = new List<string>() { "是", "否" };
                    SetGridValue(GuardTourGrid, table, configLst);
                }   
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.RoomNameControl))
                {
                    ThElectricalUIService.Instance.Parameter.RoomInfoMappingTable = table;
                }
            }
            if (ThElectricalUIService.Instance.Parameter.RoomInfoMappingTable != null)
            {
                ThElectricalUIService.Instance.Parameter.RoomInfoMappingTree = RoomConfigTreeService.CreateRoomTree(ThElectricalUIService.Instance.Parameter.RoomInfoMappingTable);
            }
        }

        /// <summary>
        /// 检查并创建默认url
        /// </summary>
        private void CheckDefaultUrl()
        {
            IOOperateService.CreateFolder(configFolderUrl);
            if (!IOOperateService.FileExist(configFileUrl))
            {
                SavaExcel(configFileUrl);
            }
        }

        /// <summary>
        /// 设置datagrid表值
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="table"></param>
        /// <param name="configs"></param>
        private void SetGridValue(DataGrid dataGrid, DataTable table, List<string> configs)
        {
            dataGrid.Columns.Clear();
            int columnCount = table.Columns.Count;
            for (int i = 0; i < columnCount; i++)
            {
                if (i >= columnCount - 2)
                {
                    DataGridComboBoxColumn column = new DataGridComboBoxColumn();
                    column.Header = table.Columns[i].ColumnName;
                    column.ItemsSource = configs;
                    column.SelectedValueBinding = new Binding(table.Columns[i].ColumnName);
                    dataGrid.Columns.Add(column);
                }
                else
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    column.Header = table.Columns[i].ColumnName;
                    column.Binding = new Binding(table.Columns[i].ColumnName);
                    column.IsReadOnly = true;
                    dataGrid.Columns.Add(column);
                }
            }
            dataGrid.AutoGenerateColumns = false;
            dataGrid.ItemsSource = table.DefaultView;
        }

        /// <summary>
        /// 设置默认值
        /// </summary>
        private void SetDefaultValue()
        {
            string[] files = Directory.GetFiles(configFolderUrl + @"\", "*.xls");
            List<string> fileLst = new List<string>();
            foreach (string file in files)
            {
                var fileArray = file.Split("\\".ToCharArray());
                fileLst.Add(fileArray[fileArray.Length - 1]);
            }
            configList.ItemsSource = fileLst;
            configList.SelectedItem = defaultFile;

            videoDistance.Text = ThElectricalUIService.Instance.Parameter.videoDistance.ToString();
            videoBlindArea.Text = ThElectricalUIService.Instance.Parameter.videoBlindArea.ToString();
            videaMaxArea.Text = ThElectricalUIService.Instance.Parameter.videaMaxArea.ToString();
            gtDistance.Text = ThElectricalUIService.Instance.Parameter.gtDistance.ToString();
            //scale.Text = ThElectricalUIService.Instance.Parameter.scale.ToString();
        }

        /// <summary>
        /// 获取配置表
        /// </summary>
        /// <param name="table"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private List<string> GetConfigCollection(DataTable table, int index)
        {
            List<string> configs = new List<string>();
            foreach (DataRow row in table.Rows)
            {
                if (!string.IsNullOrEmpty(row[index].ToString()))
                {
                    configs.Add(row[index].ToString());
                }
            }

            return configs;
        }

        /// <summary>
        /// 读取excel内容
        /// </summary>
        /// <returns></returns>
        private DataSet GetExcelContent(string path)
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            return excelSrevice.ReadExcelToDataSet(path, true);
        }

        /// <summary>
        /// 存储成excel到特定路径
        /// </summary>
        /// <param name="url"></param>
        private void SavaExcel(string url)
        {
            DataSet dataSet = new DataSet();
            dataSet.Merge(((DataView)VideoMonitoringGrid.ItemsSource).Table);
            dataSet.Merge(((DataView)IntrusionAlarmGrid.ItemsSource).Table);
            dataSet.Merge(((DataView)AccessControlGrid.ItemsSource).Table);
            dataSet.Merge(((DataView)GuardTourGrid.ItemsSource).Table);
            dataSet.Merge(configSet.Tables[ThElectricalUIService.Instance.Parameter.Configs]);
            dataSet.Merge(configSet.Tables[ThElectricalUIService.Instance.Parameter.RoomNameControl]);

            //存储成excel
            ReadExcelService excelService = new ReadExcelService();
            excelService.ConvertExcelToDataSet(dataSet, url);
        }

        #region check输入
        //private bool CheckBlockSize()
        //{

        //}

        /// <summary>
        /// 检查视频监控系统输入
        /// </summary>
        /// <returns></returns>
        private bool CheckVMSystem()
        {
            if (!CheckTextBoxValue(videoDistance.Text))
            {
                MessageBox.Show("摄像机均布间距不能为空且必须为数字");
                return false;
            }
            if (!CheckTextBoxValue(videoBlindArea.Text))
            {
                MessageBox.Show("摄像机纵向盲区距离不能为空且必须为数字");
                return false;
            }
            if (!CheckTextBoxValue(videaMaxArea.Text))
            {
                MessageBox.Show("摄像机最大成像距离不能为空且必须为数字");
                return false;
            }

            ThElectricalUIService.Instance.Parameter.videoDistance = double.Parse(videoDistance.Text);
            ThElectricalUIService.Instance.Parameter.videoBlindArea = double.Parse(videoBlindArea.Text);
            ThElectricalUIService.Instance.Parameter.videaMaxArea = double.Parse(videaMaxArea.Text);

            return true;
        }

        /// <summary>
        /// 检查电子巡更系统输入
        /// </summary>
        /// <returns></returns>
        private bool CheckGTSystem()
        {
            if (!CheckTextBoxValue(gtDistance.Text))
            {
                MessageBox.Show("电子巡更系统排布间距不能为空且必须为数字");
                return false;
            }

            ThElectricalUIService.Instance.Parameter.gtDistance = double.Parse(gtDistance.Text);
            return true;
        }

        /// <summary>
        /// 检验textbox
        /// </summary>
        private bool CheckTextBoxValue(string textString)
        {
            if (string.IsNullOrEmpty(textString))
            {
                return false;
            }

            Match m = Regex.Match(textString, @"^[0-9]*$");   // 匹配正则表达式
            if (!m.Success)   // 输入的不是数字
            {
                return false;
            }
            else   // 输入的是数字
            {
                return true;
            }
        }
        #endregion

        #region 事件
        /// <summary>
        /// 一键布置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            //聚焦到CAD
            SetFocusToDwgView();

            //发送命令
            if ((SecurityPlaneTab.SelectedItem as TabItem).Header.ToString() == ThElectricalUIService.Instance.Parameter.VideoMonitoringSystem)
            {
                if (!CheckVMSystem()) return;
                CommandHandlerBase.ExecuteFromCommandLine(false, "THVMSYSTEM");
            }
            else if ((SecurityPlaneTab.SelectedItem as TabItem).Header.ToString() == ThElectricalUIService.Instance.Parameter.IntrusionAlarmSystem)
            {
                CommandHandlerBase.ExecuteFromCommandLine(false, "THIASYSTEM");
            }
            else if ((SecurityPlaneTab.SelectedItem as TabItem).Header.ToString() == ThElectricalUIService.Instance.Parameter.AccessControlSystem)
            {
                CommandHandlerBase.ExecuteFromCommandLine(false, "THACSYSTEM");
            }
            else if ((SecurityPlaneTab.SelectedItem as TabItem).Header.ToString() == ThElectricalUIService.Instance.Parameter.GuardTourSystem)
            {
                if (!CheckGTSystem()) return;
                CommandHandlerBase.ExecuteFromCommandLine(false, "THGTSYSTEM");
            }

            this.Hide();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            //聚焦到CAD
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THSPPIPE");
            this.Hide();
        }

        /// <summary>
        /// 导入excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnImportTabl1e_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Multiselect = false;//该值确定是否可以选择多个文件
            openFileDialog.Filter = "Microsoft Excel files(*.xls)|*.xls;*.xlsx";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string file = openFileDialog.FileName;

                //设置填充listview
                var dataSet = GetExcelContent(file);
                SetListView(dataSet);

                //存储成excel
                var fileArray = file.Split("\\".ToCharArray());
                var flieName = fileArray[fileArray.Length - 1];
                string newPath = configFolderUrl + "\\" + flieName;
                ReadExcelService excelService = new ReadExcelService();
                excelService.ConvertExcelToDataSet(dataSet, newPath);

                //设置默认值
                SetDefaultValue();
                configList.SelectedItem = flieName;
            }
        }

        /// <summary>
        /// 导出excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExportTable_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Filter = "Microsoft Excel files(*.xlsx)|*.xls;*.xlsx";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var localFilePath = dialog.FileName.ToString();

                SavaExcel(localFilePath);
            }
        }

        /// <summary>
        /// 更换配置原则
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void configList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string file = configFolderUrl + "\\" + configList.SelectedItem.ToString();

            //设置填充listview
            var dataSet = GetExcelContent(file);
            SetListView(dataSet);
        }
        #endregion

        /// <summary>
        /// 聚焦到CAD
        /// </summary>
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
