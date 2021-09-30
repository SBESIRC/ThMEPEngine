using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using ThCADExtension;
using ThControlLibraryWPF.CustomControl;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPHVAC.FanLayout.Command;
using ThMEPHVAC.FanLayout.ViewModel;

namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanLayoutMainWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanLayoutMainWidget : ThCustomWindow
    {
        public uiFanLayoutMainWidget()
        {
            InitializeComponent();
            this.DataContext = new ThFanLayoutViewModel();
            ReadFanParaTable(ThCADCommon.FanParameterTablePath());
        }

        private ThFanLayoutViewModel ViewModel
        {
            get
            {
                return this.DataContext as ThFanLayoutViewModel;
            }
        }

        private void ReadFanParaTable(string path)
        {
            ObservableCollection<ThFanConfigInfo> WAFInfoList = new ObservableCollection<ThFanConfigInfo>();
            ObservableCollection<ThFanConfigInfo> WEXHInfoList = new ObservableCollection<ThFanConfigInfo>();
            ObservableCollection<ThFanConfigInfo> CEXHInfoList = new ObservableCollection<ThFanConfigInfo>();
            //读取excel表的数据
            ReadExcelService excelService = new ReadExcelService();
            var dataSet = excelService.ReadExcelToDataSet(path, false);
            var WAFDataSet = dataSet.Tables["壁式轴流风机-风量序列"];

            for (int i = 1; i < WAFDataSet.Rows.Count;i++)
            {
                var row = WAFDataSet.Rows[i] as System.Data.DataRow;

                ThFanConfigInfo fanInfo = new ThFanConfigInfo();
                fanInfo.FanNumber = (string)row[0];
                if (string.IsNullOrEmpty( fanInfo.FanNumber))
                {
                    continue;
                }
                fanInfo.FanVolume = double.Parse((string)row[2]);
                fanInfo.FanPressure = double.Parse((string)row[3]);
                fanInfo.FanPower = double.Parse((string)row[4])*1000.0;
                fanInfo.FanNoise = double.Parse((string)row[7]);
                fanInfo.FanWeight = double.Parse((string)row[8]);
                fanInfo.FanDepth = double.Parse((string)row[9]);
                fanInfo.FanWidth = double.Parse((string)row[10]);
                fanInfo.FanLength = double.Parse((string)row[11]);
                WAFInfoList.Add(fanInfo);
            }


            var WEXHDataSet = dataSet.Tables["壁式式排气扇-风量序列 "];
            for (int i = 1; i < WEXHDataSet.Rows.Count; i++)
            {
                var row = WEXHDataSet.Rows[i] as System.Data.DataRow;
                ThFanConfigInfo fanInfo = new ThFanConfigInfo();
                fanInfo.FanNumber = (string)row[0];
                if (string.IsNullOrEmpty(fanInfo.FanNumber))
                {
                    continue;
                }
                fanInfo.FanVolume = double.Parse((string)row[2]);
                fanInfo.FanPower = double.Parse((string)row[3]) * 1000.0;
                fanInfo.FanNoise = double.Parse((string)row[5]);
                fanInfo.FanWeight = double.Parse((string)row[6]);
                string size = (string)row[7];
                MatchCollection results = Regex.Matches(size, @"[\d+\.?\d*]+");

                fanInfo.FanLength = double.Parse(results[0].ToString());
                fanInfo.FanWidth = double.Parse(results[1].ToString());
                fanInfo.FanDepth = double.Parse((string)row[8]);
                WEXHInfoList.Add(fanInfo);
            }

            var CEXHDataSet = dataSet.Tables["吊顶式排气扇-风量序列"];
            for (int i = 1; i < CEXHDataSet.Rows.Count; i++)
            {
                var row = CEXHDataSet.Rows[i] as System.Data.DataRow;
                ThFanConfigInfo fanInfo = new ThFanConfigInfo();
                fanInfo.FanNumber = (string)row[0];
                if (string.IsNullOrEmpty(fanInfo.FanNumber))
                {
                    continue;
                }
                fanInfo.FanVolume = double.Parse((string)row[1]);
                fanInfo.FanPressure = double.Parse((string)row[2]);
                fanInfo.FanPower = double.Parse((string)row[3]) * 1000.0;
                fanInfo.FanNoise = double.Parse((string)row[5]);
                fanInfo.FanWeight = double.Parse((string)row[6]);
                fanInfo.FanDepth = double.Parse((string)row[10]);
                fanInfo.FanWidth = double.Parse((string)row[9]);
                fanInfo.FanLength = double.Parse((string)row[8]);
                CEXHInfoList.Add(fanInfo);
            }
            FanWAFWidget.SetFanConfigInfoList(WAFInfoList);
            FanWEXHWidget.SetFanConfigInfoList(WEXHInfoList);
            FanCEXHWidget.SetFanConfigInfoList(CEXHInfoList);
        }
        private void btnInsertFan_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.thFanLayoutConfigInfo.WAFConfigInfo = FanWAFWidget.GetFanWAFConfigInfo();
            ViewModel.thFanLayoutConfigInfo.WEXHConfigInfo = FanWEXHWidget.GetFanWEXHConfigInfo();
            ViewModel.thFanLayoutConfigInfo.CEXHConfigInfo = FanCEXHWidget.GetFanCEXHConfigInfo();
            var cmd = new ThFanLayoutExtractCmd();
            cmd.thFanLayoutConfigInfo = ViewModel.thFanLayoutConfigInfo;
            cmd.Execute();
        }
        private void btnExportMat_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var cmd = new ThFanMaterialTableExtractCmd();
            cmd.Execute();
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
