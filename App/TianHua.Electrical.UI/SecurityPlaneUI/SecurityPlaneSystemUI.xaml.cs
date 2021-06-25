using AcHelper;
using AcHelper.Commands;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.Service;
using ThMEPLighting.ServiceModels;
using TianHua.Electrical.UI.Service;

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
                    ThElectricalUIService.Instance.Parameter.guardTourSystemTable = table;
                }   
                else if (table.TableName.Contains(ThElectricalUIService.Instance.Parameter.RoomNameControl))
                {
                    ThElectricalUIService.Instance.Parameter.RoomInfoMappingTable = table;
                }
            }
        }

        /// <summary>
        /// 读取excel内容
        /// </summary>
        /// <returns></returns>
        private DataSet GetExcelContent()
        {
            ExcelSrevice excelSrevice = new ExcelSrevice();
            return excelSrevice.ReadExcelToDataSet(url, true);
        }
    }
}
