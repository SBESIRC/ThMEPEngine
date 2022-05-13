using System;
using System.Collections.Generic;

using ThMEPEngineCore.IO.ExcelService;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;

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

                if(string.IsNullOrEmpty(blockInfo.BlockName))
                {
                    continue;
                }

                // Cat.1
                column++;
                try
                {
                    var cat_1 = (ThPDSLoadTypeCat_1)Enum.Parse(typeof(ThPDSLoadTypeCat_1), StringFilter(table.Rows[row][column].ToString()));
                    blockInfo.Cat_1 = cat_1;
                }
                catch
                {
                    blockInfo.Cat_1 = ThPDSLoadTypeCat_1.LumpedLoad;
                }

                // Cat.2
                column++;
                try
                {
                    var cat_2 = (ThPDSLoadTypeCat_2)Enum.Parse(typeof(ThPDSLoadTypeCat_2), StringFilter(table.Rows[row][column].ToString()));
                    blockInfo.Cat_2 = cat_2;
                }
                catch
                {
                    blockInfo.Cat_2 = ThPDSLoadTypeCat_2.None;
                }

                // Properties(Include)
                column++;
                blockInfo.Properties = StringFilter(table.Rows[row][column].ToString());

                // Default Circuit Type
                column++;
                var defaultCircuitType = (ThPDSCircuitType)Enum.Parse(typeof(ThPDSCircuitType), StringFilter(table.Rows[row][column].ToString()));
                blockInfo.DefaultCircuitType = defaultCircuitType;

                // Remark
                column++;

                // Phase
                column++;
                blockInfo.Phase = TypeConvert(StringFilter(table.Rows[row][column].ToString()));

                // Demand Factor
                column++;
                blockInfo.DemandFactor = Convert.ToDouble(StringFilter(table.Rows[row][column].ToString()));

                // Power Factor
                column++;
                blockInfo.PowerFactor = Convert.ToDouble(StringFilter(table.Rows[row][column].ToString()));

                // Fire Load
                column++;
                if (table.Rows[row][column].Equals("True"))
                {
                    blockInfo.FireLoad = ThPDSFireLoad.FireLoad;
                }
                else if(table.Rows[row][column].Equals("False"))
                {
                    blockInfo.FireLoad = ThPDSFireLoad.NonFireLoad;
                }
                else
                {
                    blockInfo.FireLoad = ThPDSFireLoad.Unknown;
                }

                // Defult Description
                column++;
                blockInfo.DefaultDescription = StringFilter(table.Rows[row][column].ToString());

                // Cable laying method 1
                column++;
                blockInfo.CableLayingMethod1 = CableLayingMethodConvert(StringFilter(table.Rows[row][column].ToString()));

                // Cable laying method 2
                column++;
                blockInfo.CableLayingMethod2 = CableLayingMethodConvert(StringFilter(table.Rows[row][column].ToString()));

                blockInfos.Add(blockInfo);
            }
            return blockInfos;
        }

        private string StringFilter(string str)
        {
            return str.Replace(" ", "").Replace("\n", ""); ;
        }

        private ThPDSPhase TypeConvert(string value)
        {
            if(value == "1")
            {
                return ThPDSPhase.一相;
            }
            else if(value == "3")
            {
                return ThPDSPhase.三相;
            }
            else
            {
                return ThPDSPhase.None;
            }
        }

        private LayingSite CableLayingMethodConvert(string str)
        {
            switch(str)
            {
                case "CE":
                    return LayingSite.CE;
                case "SCE":
                    return LayingSite.SCE;
                case "WS":
                    return LayingSite.WS;
                case "RS":
                    return LayingSite.RS;
                case "CC":
                    return LayingSite.CC;
                case "WC":
                    return LayingSite.WC;
                case "CLC":
                    return LayingSite.CLC;
                case "BC":
                    return LayingSite.BC;
                case "FC":
                    return LayingSite.FC;
                case "AC":
                    return LayingSite.AC;
                case "AB":
                    return LayingSite.AB;
                default:
                    return LayingSite.None;
            }
        }
    }
}
