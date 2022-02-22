using System;
using System.Collections.Generic;

using ThMEPEngineCore.IO.ExcelService;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThConfigurationFileService
    {
        public List<ThPDSBlockInfo> Acquire(string loadConfigUrl)
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(loadConfigUrl, true);
            var table = dataSet.Tables[ThPDSCommon.BLOCK];
            return GetBlockTable(table);
        }

        public List<ThPDSBlockInfo> GetBlockTable(System.Data.DataTable table)
        {
            var blockInfos = new List<ThPDSBlockInfo>();
            for (int row = 0; row < table.Rows.Count; row++)
            {
                var blockInfo = new ThPDSBlockInfo();
                // Block
                var column = 0;
                blockInfo.BlockName = StringFilter(table.Rows[row][column].ToString());

                column++;
                try
                {
                    var cat_1 = (ThPDSLoadType)Enum.Parse(typeof(ThPDSLoadType), StringFilter(table.Rows[row][column].ToString()));
                    blockInfo.Cat_1 = cat_1;
                }
                catch
                {
                    blockInfo.Cat_1 = ThPDSLoadType.None;
                }

                column++;
                blockInfo.Cat_2 = StringFilter(table.Rows[row][column].ToString());

                column++;
                blockInfo.Properties = StringFilter(table.Rows[row][column].ToString());

                blockInfos.Add(blockInfo);
            }
            return blockInfos;
        }

        private string StringFilter(string str)
        {
            return str.Replace(" ", "").Replace("\n", ""); ;
        }
    }
}
