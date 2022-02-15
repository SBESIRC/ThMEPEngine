using System.Collections.Generic;

using ThMEPEngineCore.IO.ExcelService;

namespace TianHua.Electrical.PDS.Service
{
    public class ThLoadNameService
    {
        public List<string> Acquire(string loadConfigUrl)
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(loadConfigUrl, true);
            var table = dataSet.Tables[ThPDSCommond.LOAD];
            return GetBlockNames(table);
        }

        public List<string> GetBlockNames(System.Data.DataTable table)
        {
            var blockNames = new List<string>();
            var column = 0;
            for (int row = 0; row < table.Rows.Count; row++)
            {
                // 块名
                blockNames.Add(StringFilter(table.Rows[row][column].ToString()));
            }
            return blockNames;
        }

        private string StringFilter(string str)
        {
            return str.Replace(" ", "").Replace("\n", ""); ;
        }
    }
}
