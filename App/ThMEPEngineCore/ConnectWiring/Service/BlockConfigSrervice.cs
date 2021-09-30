using System.IO;
using System.Data;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.ConnectWiring.Model;

namespace ThMEPEngineCore.ConnectWiring.Service
{
    public class BlockConfigSrervice
    {
        static string blockConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "连线功能白名单.xlsx");

        /// <summary>
        /// 计算回路信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public List<WiringLoopModel> GetLoopInfo(string tableName)
        {
            var configInfos = ReadBlockConfig(tableName).Where(x => x.loops.Count > 0).ToList();
            var groupInfos = configInfos.GroupBy(x => x.loops.First()).ToList();

            List<WiringLoopModel> wiring = new List<WiringLoopModel>();
            foreach (var group in groupInfos)
            {
                WiringLoopModel loopModel = new WiringLoopModel();
                int count = group.Select(x => x.loops.Count).OrderByDescending(x => x).First();
                for (int i = 0; i < count; i++)
                {
                    var iGroup = group.Where(x => x.loops.Count > i).ToList();
                    LoopInfoModel loopInfo = new LoopInfoModel();
                    loopInfo.LineType = iGroup.First().loops[i];
                    foreach (var info in iGroup)
                    {
                        loopInfo.blockNames.Add(info.blockName);
                    }
                    loopModel.loopInfoModels.Add(loopInfo);
                }
                wiring.Add(loopModel);
            }

            return wiring;
        }

        /// <summary>
        /// 读取块名配置表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public List<BlockConfigModel> ReadBlockConfig(string tableName)
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(blockConfigUrl, true);

            DataTable table = dataSet.Tables[tableName];
            List<BlockConfigModel> blockModels = new List<BlockConfigModel>();
            for (int i = 1; i < table.Rows.Count; i++)
            {
                DataRow dataRow = table.Rows[i];
                BlockConfigModel model = new BlockConfigModel();
                model.blockName = dataRow[0].ToString();
                model.name = dataRow[1].ToString();
                for (int j = 2; j < table.Columns.Count; j++)
                {
                    if (!string.IsNullOrEmpty(dataRow[j].ToString()))
                    {
                        model.loops.Add(dataRow[j].ToString());
                    }
                }
                blockModels.Add(model);
            }

            return blockModels;
        }
    }
}
