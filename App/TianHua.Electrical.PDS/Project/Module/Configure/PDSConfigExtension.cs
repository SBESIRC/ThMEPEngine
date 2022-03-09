using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.IO.ExcelService;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    public static class PDSConfigExtension
    {
        /// <summary>
        /// 加载全局配置
        /// </summary>
        /// <param name="project"></param>
        public static void LoadGlobalConfig(this PDSProject project)
        {
            LoadBreakerConfig();
            LoadContactorConfig();
            LoadIsolatorConfig();
            LoadThermalRelayConfig();
        }

        public static List<string> GetTripDevice(this ThPDSLoadTypeCat_1 type, bool FireLoad, out string characteristics)
        {
            switch (type)
            {
                case ThPDSLoadTypeCat_1.Luminaire:
                    {
                        if (FireLoad)
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                        else
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                    }
                case ThPDSLoadTypeCat_1.Socket:
                    {
                        if (FireLoad)
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                        else
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                    }
                case ThPDSLoadTypeCat_1.Motor:
                    {
                        if (FireLoad)
                        {
                            characteristics ="D";
                            return new List<string>() { "MA" };
                        }
                        else
                        {
                            characteristics ="D";
                            return new List<string>() { "MA" };
                        }
                    }
                case ThPDSLoadTypeCat_1.DistributionPanel:
                    {
                        if (FireLoad)
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                        else
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                    }
                case ThPDSLoadTypeCat_1.LumpedLoad:
                    {
                        if (FireLoad)
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                        else
                        {
                            characteristics ="C";
                            return new List<string>() { "TM", "EL" };
                        }
                    }
                default:
                    {
                        //未支持的类型
                        throw new NotSupportedException();
                    }
            }
        }

        /// <summary>
        /// 加载断路器配置
        /// </summary>
        private static void LoadBreakerConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.CircuitBreakerUrl, true);
            var TableMCB = dataSet.Tables["MCB"];
            for (int i = 1; i < TableMCB.Rows.Count; i++)
            {
                var row = TableMCB.Rows[i];
                BreakerConfiguration.breakerComponentInfos.Add(new BreakerComponentInfo()
                {
                    ModelName = row["型号"].ToString(),
                    Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                    FrameSize = row["壳架规格"].ToString(),
                    MaxKV = row["额定电压"].ToString(),
                    Poles = row["级数"].ToString(),
                    Amps = row["额定电流"].ToString(),
                    TripDevice = row["脱扣器"].ToString(),
                    ResidualCurrent = row["剩余电流动作"].ToString(),
                    Characteristics = row["瞬时脱扣器型式"].ToString(),
                    Width = row["宽度"].ToString(),
                    Depth = row["深度"].ToString(),
                    Height = row["高度"].ToString(),
                    DefaultPick = row["默认不选"].ToString().Equals("N") ? false : true
                });
            }
            var TableMCCB = dataSet.Tables["MCCB"];
            for (int i = 1; i < TableMCCB.Rows.Count; i++)
            {
                var row = TableMCCB.Rows[i];
                BreakerConfiguration.breakerComponentInfos.Add(new BreakerComponentInfo()
                {
                    ModelName = row["型号"].ToString(),
                    Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                    FrameSize = row["壳架规格"].ToString(),
                    MaxKV = row["额定电压"].ToString(),
                    Poles = row["级数"].ToString(),
                    Amps = row["额定电流"].ToString(),
                    TripDevice = row["脱扣器"].ToString(),
                    ResidualCurrent = row["剩余电流动作"].ToString(),
                    Characteristics = row["瞬时脱扣器型式"].ToString(),
                    Width = row["宽度"].ToString(),
                    Depth = row["深度"].ToString(),
                    Height = row["高度"].ToString(),
                    DefaultPick = row["默认不选"].ToString().Equals("N") ? false : true
                });
            }
            var TableACB = dataSet.Tables["ACB"];
            for (int i = 1; i < TableACB.Rows.Count; i++)
            {
                var row = TableACB.Rows[i];
                BreakerConfiguration.breakerComponentInfos.Add(new BreakerComponentInfo()
                {
                    ModelName = row["型号"].ToString(),
                    Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                    FrameSize = row["壳架规格"].ToString(),
                    MaxKV = row["额定电压"].ToString(),
                    Poles = row["级数"].ToString(),
                    Amps = row["额定电流"].ToString(),
                    TripDevice = row["脱扣器"].ToString(),
                    ResidualCurrent = row["剩余电流动作"].ToString(),
                    Characteristics = row["瞬时脱扣器型式"].ToString(),
                    Width = row["宽度"].ToString(),
                    Depth = row["深度"].ToString(),
                    Height = row["高度"].ToString(),
                    DefaultPick = row["默认不选"].ToString().Equals("N") ? false : true
                });
            }
        }

        /// <summary>
        /// 加载接触器配置
        /// </summary>
        private static void LoadContactorConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.ContactorUrl, true);
            var TableCJ = dataSet.Tables["CJ20"];
            for (int i = 1; i < TableCJ.Rows.Count; i++)
            {
                var row = TableCJ.Rows[i];
                ContactorConfiguration.contactorInfos.Add(new ContactorComponentInfo()
                {
                    ModelName = row["型号"].ToString(),
                    MaxKV = row["额定电压"].ToString(),
                    Poles = row["级数"].ToString(),
                    Amps = double.Parse(row["额定电流"].ToString()),
                    InstallMethod = row["安装方式"].ToString(),
                    Width = row["宽度"].ToString(),
                    Depth = row["深度"].ToString(),
                    Height = row["高度"].ToString(),
                });
            }
        }
        
        /// <summary>
        /// 加载隔离开关配置
        /// </summary>
        private static void LoadIsolatorConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.IsolatorUrl, true);
            var Table = dataSet.Tables["QL"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                IsolatorConfiguration.isolatorInfos.Add(new IsolatorComponentInfo()
                {
                    ModelName = row["型号"].ToString(),
                    MaxKV = row["额定电压"].ToString(),
                    Poles = row["级数"].ToString(),
                    Amps = double.Parse(row["额定电流"].ToString()),
                    InstallMethod = row["安装方式"].ToString(),
                    Width = row["宽度"].ToString(),
                    Depth = row["深度"].ToString(),
                    Height = row["高度"].ToString(),
                });
            }
        }
        
        /// <summary>
        /// 加载热继电器配置
        /// </summary>
        private static void LoadThermalRelayConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.ThermalRelayUrl, true);
            var Table = dataSet.Tables["JR20"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    ThermalRelayConfiguration.thermalRelayInfos.Add(new ThermalRelayComponentInfo()
                    {
                        ModelName = row["型号"].ToString(),
                        MaxKV = row["额定电压"].ToString(),
                        Poles = row["级数"].ToString(),
                        MaxAmps = double.Parse(row["整定电流上限"].ToString()),
                        MinAmps = double.Parse(row["整定电流下限"].ToString()),
                        ThermalDeviceCode = row["热元件代号"].ToString(),
                        InstallMethod = row["安装方式"].ToString(),
                        Width = row["宽度"].ToString(),
                        Depth = row["深度"].ToString(),
                        Height = row["高度"].ToString(),
                    });
                }
            }
        }
    }
}
