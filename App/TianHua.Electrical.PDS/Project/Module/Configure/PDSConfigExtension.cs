using System;
using System.ArrayExtensions;
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
            LoadATSEConfig();
            LoadMTSEConfig();
            LoadConductorConfig();
            LoadCableCondiutConfig();
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
                            return new List<string>() { "MA", "EL" };
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
                            return new List<string>() { "MA", "EL" };
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
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    var currents = row["额定电流"].ToString();
                    foreach (var current in currents.Split(';'))
                    {
                        BreakerConfiguration.breakerComponentInfos.Add(new BreakerComponentInfo()
                        {
                            ModelName = row["型号"].ToString(),
                            Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                            FrameSize = row["壳架规格"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
                            TripDevice = row["脱扣器"].ToString(),
                            ResidualCurrent = row["剩余电流动作"].ToString(),
                            Characteristics = row["瞬时脱扣器型式"].ToString(),
                            Width = row["宽度"].ToString(),
                            Depth = row["深度"].ToString(),
                            Height = row["高度"].ToString(),
                            DefaultPick = row["默认不选"].ToString().Equals("N") ? false : true
                        });
                    };
                }
            }
            var TableMCCB = dataSet.Tables["MCCB"];
            for (int i = 1; i < TableMCCB.Rows.Count; i++)
            {
                var row = TableMCCB.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    var currents = row["额定电流"].ToString();
                    foreach (var current in currents.Split(';'))
                    {
                        BreakerConfiguration.breakerComponentInfos.Add(new BreakerComponentInfo()
                        {
                            ModelName = row["型号"].ToString(),
                            Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                            FrameSize = row["壳架规格"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
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
            }
            var TableACB = dataSet.Tables["ACB"];
            for (int i = 1; i < TableACB.Rows.Count; i++)
            {
                var row = TableACB.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    var currents = row["额定电流"].ToString();
                    foreach (var current in currents.Split(';'))
                    {
                        BreakerConfiguration.breakerComponentInfos.Add(new BreakerComponentInfo()
                        {
                            ModelName = row["型号"].ToString(),
                            Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                            FrameSize = row["壳架规格"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
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

        /// <summary>
        /// 加载ATSE配置
        /// </summary>
        private static void LoadATSEConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.ATSEUrl, true);
            var Table = dataSet.Tables["ATSE"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    ATSEConfiguration.ATSEComponentInfos.Add(new ATSEComponentInfo()
                    {
                        ModelName = row["型号"].ToString(),
                        Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                        FrameSize = row["壳架等级"].ToString(),
                        MaxKV = row["额定电压"].ToString(),
                        Poles = row["级数"].ToString(),
                        Amps = row["额定电流"].ToString(),
                        ATSECharacteristics = row["ATSE功能特点"].ToString(),
                        UtilizationCategory = row["使用类别"].ToString(),
                        ATSEMainContact = row["主触头O位置"].ToString(),
                        Icu = row["额定极限分断能力"].ToString(),
                        IcuMultiple = row["额定运行短路分断能力倍数"].ToString(),
                        Icm = row["额定短路接通能力"].ToString(),
                        Icw = row["额定短时耐受能力"].ToString(),
                        Tkr = row["短时耐受时间"].ToString(),
                        TrippingTime = row["瞬时脱扣时间"].ToString(),
                        TrippingTimeDelay = row["延时脱扣时间"].ToString(),
                        WiringCapacity = row["接线能力"].ToString(),
                        InstallMethod = row["安装方式"].ToString(),
                        Width = row["宽度"].ToString(),
                        Depth = row["深度"].ToString(),
                        Height = row["高度"].ToString(),
                    });
                }
            }
        }

        /// <summary>
        /// 加载MTSE配置
        /// </summary>
        private static void LoadMTSEConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.MTSEUrl, true);
            var Table = dataSet.Tables["MTSE"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    MTSEConfiguration.MTSEComponentInfos.Add(new MTSEComponentInfo()
                    {
                        ModelName = row["型号"].ToString(),
                        Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                        FrameSize = row["壳架等级"].ToString(),
                        MaxKV = row["额定电压"].ToString(),
                        Poles = row["级数"].ToString(),
                        Amps = row["额定电流"].ToString(),
                        MTSECharacteristics = row["MTSE功能特点"].ToString(),
                        UtilizationCategory = row["使用类别"].ToString(),
                        MTSEMainContact = row["主触头O位置"].ToString(),
                        Icu = row["额定极限分断能力"].ToString(),
                        IcuMultiple = row["额定运行短路分断能力倍数"].ToString(),
                        Icm = row["额定短路接通能力"].ToString(),
                        Icw = row["额定短时耐受能力"].ToString(),
                        Tkr = row["短时耐受时间"].ToString(),
                        TrippingTime = row["瞬时脱扣时间"].ToString(),
                        TrippingTimeDelay = row["延时脱扣时间"].ToString(),
                        WiringCapacity = row["接线能力"].ToString(),
                        InstallMethod = row["安装方式"].ToString(),
                        Width = row["宽度"].ToString(),
                        Depth = row["深度"].ToString(),
                        Height = row["高度"].ToString(),
                    });
                }
            }
        }
        
        /// <summary>
        /// 加载导体配置
        /// </summary>
        private static void LoadConductorConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.ConductorUrl, true);
            var Table = dataSet.Tables["电缆"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    ConductorConfigration.CableConductorInfos.Add(new ConductorComponentInfo()
                    {
                        Iset = double.Parse(row["整定电流"].ToString()),
                        Sphere = double.Parse(row["相线截面"].ToString()),
                        NumberOfPhaseWire = int.Parse(row["相线数"].ToString()),
                    });
                }
            }
            var Table1 = dataSet.Tables["电线"];
            for (int i = 1; i < Table1.Rows.Count; i++)
            {
                var row = Table1.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    ConductorConfigration.WireConductorInfos.Add(new ConductorComponentInfo()
                    {
                        Iset = double.Parse(row["整定电流"].ToString()),
                        Sphere = double.Parse(row["相线截面"].ToString()),
                        NumberOfPhaseWire = int.Parse(row["相线数"].ToString()),
                    });
                }
            }
        }
        
        /// <summary>
        /// 加载电线电缆配置
        /// </summary>
        private static void LoadCableCondiutConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectGlobalConfiguration.CableCondiutUrl, true);
            var Table = dataSet.Tables["YJY(YJV)"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                string value;
                if (!row[1].ToString().IsNullOrWhiteSpace())
                {
                    CableCondiutConfiguration.CableInfos.Add(new CableCondiutInfo()
                    {
                        FireCoating = row["耐火外护套"].ToString().Equals("Y"),
                        WireSphere = double.Parse(row["相线截面"].ToString()),
                        Phase = row["相数"].ToString() + "P",
                        DIN_SC = (value = row["SC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1:int.Parse(value),
                        DIN_JDG = (value = row["JDG穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1:int.Parse(value),
                        DIN_PC = (value = row["PC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1:int.Parse(value),
                    });
                }
            }
            var Table1 = dataSet.Tables["BYJ(BV)"];
            for (int i = 1; i < Table1.Rows.Count; i++)
            {
                var row = Table1.Rows[i];
                string value;
                if (!row[1].ToString().IsNullOrWhiteSpace())
                {
                    CableCondiutConfiguration.CondiutInfos.Add(new CableCondiutInfo()
                    {
                        FireCoating = row["耐火外护套"].ToString().Equals("Y"),
                        WireSphere = double.Parse(row["相线截面"].ToString()),
                        Phase = row["相数"].ToString() + "P",
                        DIN_SC = (value = row["SC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                        DIN_JDG = (value = row["JDG穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                        DIN_PC = (value = row["PC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                    });
                }
            }
        }
    }
}
