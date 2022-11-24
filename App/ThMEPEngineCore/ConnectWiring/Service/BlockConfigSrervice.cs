using System.IO;
using System.Data;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.ConnectWiring.Model;
using System;

namespace ThMEPEngineCore.ConnectWiring.Service
{
    public class BlockConfigSrervice
    {
        static string blockConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "连线功能白名单.xlsx");
        string loopName = "回路";
        string shape = "形状分类";
        string xRight = "X+";
        string xLeft = "X-";
        string yRight = "Y+";
        string yLeft = "Y-";
        string installMethod = "Install Method";
        string density = "Density";

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
                    loopInfo.LineContent = iGroup.First().loops[i];
                    foreach (var info in iGroup)
                    {
                        LoopBlockInfos block = new LoopBlockInfos();
                        block.blockName = info.blockName;
                        block.blcokShape = info.blcokShape;
                        block.XRight = info.XRight;
                        block.XLeft = info.XLeft;
                        block.YRight = info.YRight;
                        block.YLeft = info.YLeft;
                        block.InstallMethod = GetInstallMethod(info.InstallMethod);
                        block.Density = info.Density;
                        loopInfo.blocks.Add(block);
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
            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow dataRow = table.Rows[i];
                BlockConfigModel model = new BlockConfigModel();
                model.blockName = dataRow[0].ToString();
                model.name = dataRow[1].ToString();
                for (int j = 2; j < table.Columns.Count; j++)
                {
                    if (!string.IsNullOrEmpty(dataRow[j].ToString()))
                    {
                        if (table.Columns[j].ColumnName.Contains(loopName))
                        {
                            model.loops.Add(dataRow[j].ToString());
                        }
                        else if (table.Columns[j].ColumnName == shape)
                        {
                            model.blcokShape = (BlockShape)Enum.Parse(typeof(BlockShape), dataRow[j].ToString());
                        }
                        else if (table.Columns[j].ColumnName == xRight)
                        {
                            model.XRight = Int32.Parse(dataRow[j].ToString());
                        }
                        else if (table.Columns[j].ColumnName == xLeft)
                        {
                            model.XLeft = Int32.Parse(dataRow[j].ToString()); 
                        }
                        else if (table.Columns[j].ColumnName == yRight)
                        {
                            model.YRight = Int32.Parse(dataRow[j].ToString());
                        }
                        else if (table.Columns[j].ColumnName == yLeft)
                        {
                            model.YLeft = Int32.Parse(dataRow[j].ToString());
                        }
                        else if (table.Columns[j].ColumnName == installMethod)
                        {
                            model.InstallMethod = dataRow[j].ToString();
                        }
                        else if (table.Columns[j].ColumnName == density)
                        {
                            model.Density = Int32.Parse(dataRow[j].ToString());
                        }
                    }
                }
                blockModels.Add(model);
            }

            return blockModels;
        }

        /// <summary>
        /// 获取安装方式
        /// </summary>
        /// <param name="install"></param>
        /// <returns></returns>
        private string GetInstallMethod(string install)
        {
            if (install == "W")
            {
                return "WallMounted";
            }
            else if (install == "F")
            {
                return "Ground";
            }
            else
            {
                return "Hoisting";
            }
        }
    }
}
