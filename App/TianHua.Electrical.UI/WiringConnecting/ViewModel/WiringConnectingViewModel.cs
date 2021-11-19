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
            ReadExcelService excelSrevice = new ReadExcelService();
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
    }
}
