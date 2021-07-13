using AcHelper;
using AcHelper.Commands;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ThCADExtension;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.Service;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO.ExcelService;

namespace TianHua.Electrical.UI.SecurityPlaneUI
{
    /// <summary>
    /// uiEvaIndicatorSign.xaml 的交互逻辑
    /// </summary>
    public partial class SecurityPlaneSystemUI : ThCustomWindow
    {
        readonly string url = Path.Combine(ThCADCommon.SupportPath(), "上海地区住宅-安防配置表.xlsx");
        public SecurityPlaneSystemUI()
        {
            InitializeComponent();
            SetListView();
        }

        /// <summary>
        /// 填充listView
        /// </summary>
        private void SetListView()
        {
            var dataSet = GetExcelContent();

            foreach (DataTable table in dataSet.Tables)
            {
                if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.VideoMonitoringSystem))
                {
                    VideoMonitoringGrid.ItemsSource = table.DefaultView;
                    //VideoMonitoringGrid.Columns[0].IsReadOnly = true;
                    ThElectricalUIService.Instance.Parameter.videoMonitoringSystemTable = table;
                }
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.IntrusionAlarmSystem))
                {
                    IntrusionAlarmGrid.ItemsSource = table.DefaultView;
                    ThElectricalUIService.Instance.Parameter.intrusionAlarmSystemTable = table;
                }
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.AccessControlSystem))
                {
                    AccessControlGrid.ItemsSource = table.DefaultView;
                    ThElectricalUIService.Instance.Parameter.accessControlSystemTable = table;
                }
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.GuardTourSystem))
                {
                    GuardTourGrid.ItemsSource = table.DefaultView;
                    ThElectricalUIService.Instance.Parameter.guardTourSystemTable = table;
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
        /// 读取excel内容
        /// </summary>
        /// <returns></returns>
        private DataSet GetExcelContent()
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            return excelSrevice.ReadExcelToDataSet(url, true);
        }

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

            this.Close();
        }

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
