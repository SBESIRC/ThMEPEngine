using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.IO.ExcelService;

namespace TianHua.Electrical.UI.WiringConnecting.ViewModel
{
    public partial class WiringConnectingViewModel : NotifyPropertyChangedBase
    {
        static string roomConfigUrl = ThCADCommon.SupportPath() + "\\连线回路配置表.xlsx";
        ReadExcelService excelSrevice = new ReadExcelService();
        private ObservableCollection<LoopCinfg> _configLst = new ObservableCollection<LoopCinfg>();
        public WiringConnectingViewModel()
        {
            _configLst.Clear();
            var ds = GetExcelContent(roomConfigUrl);
            foreach (DataTable table in ds.Tables)
            {
                _configLst.Add(ConvertToModel(table));
            }
        }

        public void UpdateDataSource()
        {
            var dataset = ConvertToDataset();
            excelSrevice.ConvertDataSetToExcel(dataset, roomConfigUrl);
        }

        public ObservableCollection<LoopCinfg> configLst
        {
            get { return _configLst; }
            set
            {
                _configLst = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 读取excel内容
        /// </summary>
        /// <returns></returns>
        private DataSet GetExcelContent(string path)
        {
            return excelSrevice.ReadExcelToDataSet(path, true);
        }

        /// <summary>
        /// 将datatable转成model
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private LoopCinfg ConvertToModel(DataTable dt)
        {
            LoopCinfg loopConfig = new LoopCinfg();
            loopConfig.configModels = new ObservableCollection<ConfigModel>();
            loopConfig.systemType = dt.TableName;
            foreach (DataRow dr in dt.Rows)
            {
                ConfigModel model = new ConfigModel();
                model.loopType = dr[ConfigModel.loopTypeColumn].ToString();
                model.layerType = dr[ConfigModel.layerTypeColumn].ToString();
                model.pointNum = dr[ConfigModel.pointNumColumn].ToString();
                loopConfig.configModels.Add(model);
            }
            return loopConfig;
        }

        /// <summary>
        /// 将数据转换成dataset
        /// </summary>
        /// <returns></returns>
        private DataSet ConvertToDataset()
        {
            DataSet dataSet = new DataSet();
            foreach (var config in configLst)
            {
                DataTable dt = new DataTable();
                dt.TableName = config.systemType;
                dt.Columns.Add(ConfigModel.loopTypeColumn);
                dt.Columns.Add(ConfigModel.layerTypeColumn);
                dt.Columns.Add(ConfigModel.pointNumColumn);
                foreach (var model in config.configModels)
                {
                    DataRow dr = dt.NewRow();
                    dr[ConfigModel.loopTypeColumn] = model.loopType;
                    dr[ConfigModel.layerTypeColumn] = model.layerType;
                    dr[ConfigModel.pointNumColumn] = model.pointNum;
                    dt.Rows.Add(dr);
                }
                dataSet.Tables.Add(dt);
            }

            return dataSet;
        }
    }
}
