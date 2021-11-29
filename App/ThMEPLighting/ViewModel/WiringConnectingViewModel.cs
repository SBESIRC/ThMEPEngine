using System.IO;
using System.Data;
using ThCADExtension;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.IO.IOService;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPLighting.ServiceModels;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPLighting.ViewModel
{
    public class WiringConnectingViewModel : NotifyPropertyChangedBase
    {
        static string roomConfigUrl = (string)AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX") + "\\连线回路配置表.xlsx";
        static string supportUrl = Path.Combine(ThCADCommon.SupportPath(), "连线回路配置表.xlsx");
        ReadExcelService excelSrevice = new ReadExcelService();
        private ObservableCollection<LoopConfig> _configLst = new ObservableCollection<LoopConfig>();
        public WiringConnectingViewModel()
        {
            _configLst.Clear();
            if (!IOOperateService.FileExist(roomConfigUrl))
            {
                IOOperateService.CreateNewFile(supportUrl, roomConfigUrl);
            }

            DataSet ds = GetExcelContent(roomConfigUrl);
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

        public ObservableCollection<LoopConfig> configLst
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
        private LoopConfig ConvertToModel(DataTable dt)
        {
            LoopConfig loopConfig = new LoopConfig();
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
