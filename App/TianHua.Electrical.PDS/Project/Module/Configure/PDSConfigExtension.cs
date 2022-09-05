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
            LoadMotorConfig();
            LoadDistributionMeteringConfig();
            LoadMeterConfig();
            LoadMTConfig();
            LoadCPSConfig();
            LoadOUVPConfig();
            LoadSecondaryCircuitConfig();
            LoadCircuitConfig();
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
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.CircuitBreakerUrl, true);
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
                            Model =(BreakerModel)Enum.Parse(typeof(BreakerModel), System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", "")),
                            FrameSize = row["壳架规格"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
                            TripDevice = row["脱扣器"].ToString(),
                            ResidualCurrent = row["剩余电流动作"].ToString(),
                            Characteristics = row["瞬时脱扣器型式"].ToString(),
                            RCDCharacteristics = row["剩余电流脱扣器类型"].ToString(),
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
                            Model = (BreakerModel)Enum.Parse(typeof(BreakerModel), System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", "")),
                            FrameSize = row["壳架规格"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
                            TripDevice = row["脱扣器"].ToString(),
                            ResidualCurrent = row["剩余电流动作"].ToString(),
                            Characteristics = row["瞬时脱扣器型式"].ToString(),
                            RCDCharacteristics = row["剩余电流脱扣器类型"].ToString(),
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
                            Model = (BreakerModel)Enum.Parse(typeof(BreakerModel), System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", "")),
                            FrameSize = row["壳架规格"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
                            TripDevice = row["脱扣器"].ToString(),
                            ResidualCurrent = row["剩余电流动作"].ToString(),
                            Characteristics = row["瞬时脱扣器型式"].ToString(),
                            RCDCharacteristics = String.Empty,
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
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.ContactorUrl, true);
            var TableCJ = dataSet.Tables["CJ20"];
            for (int i = 1; i < TableCJ.Rows.Count; i++)
            {
                var row = TableCJ.Rows[i];
                ContactorConfiguration.contactorInfos.Add(new ContactorConfigurationItem()
                {
                    Model = row["型号"].ToString(),
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
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.IsolatorUrl, true);
            var Table = dataSet.Tables["QL"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                IsolatorConfiguration.isolatorInfos.Add(new IsolatorConfigurationItem()
                {
                    Model = row["型号"].ToString(),
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
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.ThermalRelayUrl, true);
            var Table = dataSet.Tables["JR20"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    ThermalRelayConfiguration.thermalRelayInfos.Add(new ThermalRelayConfigurationItem()
                    {
                        Model = row["型号"].ToString(),
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
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.ATSEUrl, true);
            var Table = dataSet.Tables["ATSE"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    var currents = row["额定电流"].ToString();
                    foreach (var current in currents.Split(';'))
                    {
                        ATSEConfiguration.ATSEComponentInfos.Add(new ATSEComponentInfo()
                        {
                            ModelName = row["型号"].ToString(),
                            Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                            FrameSize = row["壳架等级"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
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
        }

        /// <summary>
        /// 加载MTSE配置
        /// </summary>
        private static void LoadMTSEConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.MTSEUrl, true);
            var Table = dataSet.Tables["MTSE"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    var currents = row["额定电流"].ToString();
                    foreach (var current in currents.Split(';'))
                    {
                        MTSEConfiguration.MTSEComponentInfos.Add(new MTSEComponentInfo()
                        {
                            ModelName = row["型号"].ToString(),
                            Model =System.Text.RegularExpressions.Regex.Replace(row["型号"].ToString(), @"\d", ""),
                            FrameSize = row["壳架等级"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
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
        }
        
        /// <summary>
        /// 加载导体配置
        /// </summary>
        private static void LoadConductorConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.ConductorUrl, true);
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
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.CableCondiutUrl, true);
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

            var Table2 = dataSet.Tables["KYJY(KYJV)"];
            for (int i = 1; i < Table2.Rows.Count; i++)
            {
                var row = Table2.Rows[i];
                string value;
                if (!row[1].ToString().IsNullOrWhiteSpace())
                {
                    CableCondiutConfiguration.KYJY.Add(new ControlCableCondiutInfo()
                    {
                        WireSphere = double.Parse(row["控制线导体截面"].ToString()),
                        NumberOfWires = int.Parse(row["控制线芯数"].ToString()),
                        DIN_SC = (value = row["SC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                        DIN_JDG = (value = row["JDG穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                        DIN_PC = (value = row["PC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                    });
                }
            }
            var Table3 = dataSet.Tables["RYJ"];
            for (int i = 1; i < Table3.Rows.Count; i++)
            {
                var row = Table3.Rows[i];
                string value;
                if (!row[1].ToString().IsNullOrWhiteSpace())
                {
                    CableCondiutConfiguration.RYJ.Add(new ControlCableCondiutInfo()
                    {
                        WireSphere = double.Parse(row["控制线导体截面"].ToString()),
                        NumberOfWires = int.Parse(row["控制线芯数"].ToString()),
                        DIN_SC = (value = row["SC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                        DIN_JDG = (value = row["JDG穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                        DIN_PC = (value = row["PC穿管管径"].ToString()).IsNullOrWhiteSpace() ? -1 : int.Parse(value),
                    });
                }
            }
        }

        /// <summary>
        /// 加载电机配置
        /// </summary>
        private static void LoadMotorConfig()
        {
            var excelSrevice = new ReadExcelService();
            //非消防
            {
                var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.MotorTypeOneCoordinationUrl, true);
                var Table = dataSet.Tables["电动机（分立元件）"];
                for (int i = 1; i < Table.Rows.Count; i++)
                {
                    var row = Table.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.NonFire_DiscreteComponentsInfos.Add(new Motor_DiscreteComponentsInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CB = row["断路器规格"].ToString(),
                            QAC = row["接触器规格"].ToString(),
                            KH = row["热继电器规格"].ToString(),
                            Conductor = row["导体根数x每根导体截面积"].ToString(),
                        });
                    }
                }

                var Table1 = dataSet.Tables["电动机（CPS）"];
                for (int i = 1; i < Table1.Rows.Count; i++)
                {
                    var row = Table1.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.NonFire_CPSInfos.Add(new Motor_CPS()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CPS = row["CPS规格"].ToString(),
                            Conductor = row["导体根数x每根导体截面积"].ToString(),
                        });
                    }
                }

                var Table2 = dataSet.Tables["电动机（分立元件星三角启动）"];
                for (int i = 1; i < Table2.Rows.Count; i++)
                {
                    var row = Table2.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.NonFire_DiscreteComponentsStarTriangleStartInfos.Add(new Motor_DiscreteComponentsStarTriangleStartInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CB = row["断路器规格"].ToString(),
                            QAC1 = row[4].ToString(),
                            QAC2 = row[5].ToString(),
                            QAC3 = row[6].ToString(),
                            KH = row["热继电器规格"].ToString(),
                            Conductor1 = row[8].ToString(),
                            Conductor2 = row[9].ToString(),
                        });
                    }
                }

                var Table3 = dataSet.Tables["电动机（CPS星三角启动）"];
                for (int i = 1; i < Table3.Rows.Count; i++)
                {
                    var row = Table3.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.NonFire_CPSStarTriangleStartInfos.Add(new Motor_CPSStarTriangleStartInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CPS = row["CPS规格"].ToString(),
                            QAC1 = row[4].ToString(),
                            QAC2 = row[5].ToString(),
                            Conductor1 = row[6].ToString(),
                            Conductor2 = row[7].ToString(),
                        });
                    }
                }

                var Table4 = dataSet.Tables["双速电动机（分立元件D-YY）"];
                for (int i = 1; i < Table4.Rows.Count; i++)
                {
                    var row = Table4.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.NonFire_TwoSpeedMotor_DiscreteComponentsDYYCircuitInfos.Add(new TwoSpeedMotor_DiscreteComponentsDYYInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            QAC3 = row["接触器规格3"].ToString(),
                            Conductor1 = row["导体根数x每根导体截面积1"].ToString(),
                            Conductor2 = row["导体根数x每根导体截面积2"].ToString(),
                        });
                    }
                }

                var Table5 = dataSet.Tables["双速电动机（分立元件Y-Y）"];
                for (int i = 1; i < Table5.Rows.Count; i++)
                {
                    var row = Table5.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.NonFire_TwoSpeedMotor_DiscreteComponentsYYCircuitInfos.Add(new TwoSpeedMotor_DiscreteComponentsYYInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            Conductor1 = row[3].ToString(),
                            Conductor2 = row[4].ToString(),
                        });
                    }
                }

                var Table6 = dataSet.Tables["双速电动机（CPS）"];
                for (int i = 1; i < Table6.Rows.Count; i++)
                {
                    var row = Table6.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.NonFire_TwoSpeedMotor_CPSInfos.Add(new TwoSpeedMotor_CPSInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CPS = row["CPS规格"].ToString(),
                            QAC = row["接触器规格"].ToString(),
                            Conductor1 = row["导体根数x每根导体截面积1"].ToString(),
                            Conductor2 = row["导体根数x每根导体截面积2"].ToString(),
                        });
                    }
                }
            }
            //消防
            {
                var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.MotorTypeTwoCoordinationUrl, true);
                var Table = dataSet.Tables["电动机（分立元件）"];
                for (int i = 1; i < Table.Rows.Count; i++)
                {
                    var row = Table.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.Fire_DiscreteComponentsInfos.Add(new Motor_DiscreteComponentsInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CB = row["断路器规格"].ToString(),
                            QAC = row["接触器规格"].ToString(),
                            KH = row["热继电器规格"].ToString(),
                            Conductor = row["导体根数x每根导体截面积"].ToString(),
                        });
                    }
                }

                var Table1 = dataSet.Tables["电动机（CPS）"];
                for (int i = 1; i < Table1.Rows.Count; i++)
                {
                    var row = Table1.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.Fire_CPSInfos.Add(new Motor_CPS()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CPS = row["CPS规格"].ToString(),
                            Conductor = row["导体根数x每根导体截面积"].ToString(),
                        });
                    }
                }

                var Table2 = dataSet.Tables["电动机（分立元件星三角启动）"];
                for (int i = 1; i < Table2.Rows.Count; i++)
                {
                    var row = Table2.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.Fire_DiscreteComponentsStarTriangleStartInfos.Add(new Motor_DiscreteComponentsStarTriangleStartInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CB = row["断路器规格"].ToString(),
                            QAC1 = row[4].ToString(),
                            QAC2 = row[5].ToString(),
                            QAC3 = row[6].ToString(),
                            KH = row["热继电器规格"].ToString(),
                            Conductor1 = row[8].ToString(),
                            Conductor2 = row[9].ToString(),
                        });
                    }
                }

                var Table3 = dataSet.Tables["电动机（CPS星三角启动）"];
                for (int i = 1; i < Table3.Rows.Count; i++)
                {
                    var row = Table3.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.Fire_CPSStarTriangleStartInfos.Add(new Motor_CPSStarTriangleStartInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CPS = row["CPS规格"].ToString(),
                            QAC1 = row[4].ToString(),
                            QAC2 = row[5].ToString(),
                            Conductor1 = row[6].ToString(),
                            Conductor2 = row[7].ToString(),
                        });
                    }
                }

                var Table4 = dataSet.Tables["双速电动机（分立元件D-YY）"];
                for (int i = 1; i < Table4.Rows.Count; i++)
                {
                    var row = Table4.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.Fire_TwoSpeedMotor_DiscreteComponentsDYYCircuitInfos.Add(new TwoSpeedMotor_DiscreteComponentsDYYInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            QAC3 = row["接触器规格3"].ToString(),
                            Conductor1 = row["导体根数x每根导体截面积1"].ToString(),
                            Conductor2 = row["导体根数x每根导体截面积2"].ToString(),
                        });
                    }
                }

                var Table5 = dataSet.Tables["双速电动机（分立元件Y-Y）"];
                for (int i = 1; i < Table5.Rows.Count; i++)
                {
                    var row = Table5.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.Fire_TwoSpeedMotor_DiscreteComponentsYYCircuitInfos.Add(new TwoSpeedMotor_DiscreteComponentsYYInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            Conductor1 = row[3].ToString(),
                            Conductor2 = row[4].ToString(),
                        });
                    }
                }

                var Table6 = dataSet.Tables["双速电动机（CPS）"];
                for (int i = 1; i < Table6.Rows.Count; i++)
                {
                    var row = Table6.Rows[i];
                    if (!row[0].ToString().IsNullOrWhiteSpace())
                    {
                        MotorConfiguration.Fire_TwoSpeedMotor_CPSInfos.Add(new TwoSpeedMotor_CPSInfo()
                        {
                            InstalledCapacity = double.Parse(row["电机功率"].ToString()),
                            CalculateCurrent = double.Parse(row["计算电流"].ToString()),
                            StartingCurrent = double.Parse(row["启动电流"].ToString()),
                            CPS = row["CPS规格"].ToString(),
                            QAC = row["接触器规格"].ToString(),
                            Conductor1 = row["导体根数x每根导体截面积1"].ToString(),
                            Conductor2 = row["导体根数x每根导体截面积2"].ToString(),
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 加载电表箱出线回路配置
        /// </summary>
        private static void LoadDistributionMeteringConfig()
        {
            DistributionMeteringConfiguration.ShanghaiResidential.Add(new ShanghaiResidentialInfo()
            {
                LowPower = 0,
                HighPower = 6,
                Phase = ThPDSPhase.一相,
                CB1 = new List<string>() { "63A/1P" },
                MT = new List<string>() { "5(40)A", "5(60)A" },
                CB2 = new List<string>() { "40A/2P", "40A/1P+N" },
                Conductor = "2x16+E16",
            });
            DistributionMeteringConfiguration.ShanghaiResidential.Add(new ShanghaiResidentialInfo()
            {
                LowPower = 6,
                HighPower = 8,
                Phase = ThPDSPhase.一相,
                CB1 = new List<string>() { "63A/1P" },
                MT = new List<string>() { "5(40)A", "5(60)A" },
                CB2 = new List<string>() { "40A/2P", "40A/1P+N" },
                Conductor = "2x16+E16",
            });
            DistributionMeteringConfiguration.ShanghaiResidential.Add(new ShanghaiResidentialInfo()
            {
                LowPower = 8,
                HighPower = 12,
                Phase = ThPDSPhase.一相,
                CB1 = new List<string>() { "80A/1P" },
                MT = new List<string>() { "5(60)A" },
                CB2 = new List<string>() { "63A/2P", "63A/1P+N" },
                Conductor = "2x25+E16",
            });
            DistributionMeteringConfiguration.ShanghaiResidential.Add(new ShanghaiResidentialInfo()
            {
                LowPower = 12,
                HighPower = 20,
                Phase = ThPDSPhase.三相,
                CB1 = new List<string>() { "63A/3P" },
                MT = new List<string>() { "3x5(60)A" },
                CB2 = new List<string>() { "40A/4P", "40A/3P+N" },
                Conductor = "4x16+E16",
            });
            DistributionMeteringConfiguration.ShanghaiResidential.Add(new ShanghaiResidentialInfo()
            {
                LowPower = 20,
                HighPower = 29,
                Phase = ThPDSPhase.三相,
                CB1 = new List<string>() { "80A/3P" },
                MT = new List<string>() { "3x5(60)A" },
                CB2 = new List<string>() { "63A/4P", "63A/3P+N" },
                Conductor = "4x25+E16",
            });
            DistributionMeteringConfiguration.ShanghaiResidential.Add(new ShanghaiResidentialInfo()
            {
                LowPower = 29,
                HighPower = 49,
                Phase = ThPDSPhase.三相,
                CB1 = new List<string>() { "100A/3P" },
                MT = new List<string>() { "3x10(100)A", "3x20(100)A" },
                CB2 = new List<string>() { "80A/4P", "80A/3P+N" },
                Conductor = "4x25+E16",
            });

            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 0,
                HighPower = 8,
                Phase = ThPDSPhase.一相,
                CB = new List<string>() { "40A/2P", "40A/1P+N" },
                Conductor = "2x10+E10",
            });
            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 8,
                HighPower = 12,
                Phase = ThPDSPhase.一相,
                CB = new List<string>() { "63A/2P/ST", "63A/1P+N/ST" },
                Conductor = "2x16+E16",
            });
            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 12,
                HighPower = 16,
                Phase = ThPDSPhase.一相,
                CB = new List<string>() { "80A/2P/ST", "80A/1P+N/ST" },
                Conductor = "2x25+E16",
            });
            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 16,
                HighPower = 20,
                Phase = ThPDSPhase.一相,
                CB = new List<string>() { "100A/2P/ST", "100A/1P+N/ST" },
                Conductor = "2x25+E16",
            });
            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 20,
                HighPower = 26,
                Phase = ThPDSPhase.三相,
                CB = new List<string>() { "40A/4P", "40A/3P+N" },
                Conductor = "4x10+E10",
            });
            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 26,
                HighPower = 40,
                Phase = ThPDSPhase.三相,
                CB = new List<string>() { "63A/4P/ST", "63A/3P+N/ST" },
                Conductor = "4x16+E16",
            });
            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 40,
                HighPower = 53,
                Phase = ThPDSPhase.三相,
                CB = new List<string>() { "80A/4P/ST", "80A/3P+N/ST" },
                Conductor = "4x25+E16",
            });
            DistributionMeteringConfiguration.JiangsuResidential.Add(new JiangsuResidentialInfo()
            {
                LowPower = 53,
                HighPower = 66,
                Phase = ThPDSPhase.三相,
                CB = new List<string>() { "100A/4P/ST", "100A/3P+N/ST" },
                Conductor = "4x25+E16",
            });
        }

        /// <summary>
        /// 加载电能表配置
        /// </summary>
        private static void LoadMeterConfig()
        {
            MeterTransformerConfiguration.MeterComponentInfos = new List<MTComponentInfo>()
            {
                new MTComponentInfo(){ Amps = 6, parameter = "1.5(6)"},
                new MTComponentInfo(){ Amps = 8, parameter = "2(8)"},
                new MTComponentInfo(){ Amps = 10, parameter = "2.5(10)"},
                new MTComponentInfo(){ Amps = 12, parameter = "3(12)"},
                new MTComponentInfo(){ Amps = 20, parameter = "5(20)"},
                new MTComponentInfo(){ Amps = 20, parameter = "5(40)"},
                new MTComponentInfo(){ Amps = 40, parameter = "10(40)"},
                new MTComponentInfo(){ Amps = 60, parameter = "5(60)"},
                new MTComponentInfo(){ Amps = 60, parameter = "15(60)"},
                new MTComponentInfo(){ Amps = 80, parameter = "20(80)"},
                new MTComponentInfo(){ Amps = 100, parameter = "10(100)"},
                new MTComponentInfo(){ Amps = 100, parameter = "20(100)"},
                new MTComponentInfo(){ Amps = 100, parameter = "30(100)"},
            };
        }

        /// <summary>
        /// 加载电流互感器配置
        /// </summary>
        private static void LoadMTConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.CurrentTransformerUrl, true);
            var Table = dataSet.Tables["Sheet1"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    CurrentTransformerConfiguration.CTComponentInfos.Add(new CTComponentInfo()
                    {
                        Amps = double.Parse(row[0].ToString()),
                        parameter = $"{row[0]}/{row[1]}",
                    });
                }
            }
        }

        /// <summary>
        /// 加载CPS配置
        /// </summary>
        private static void LoadCPSConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.CPSUrl, true);
            var Table = dataSet.Tables["CPS"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    var currents = row["额定电流"].ToString();
                    foreach (var current in currents.Split(';'))
                    {
                        CPSConfiguration.CPSComponentInfos.Add(new CPSComponentInfo()
                        {
                            Model = row["型号"].ToString(),
                            FrameSize = row["壳架规格"].ToString(),
                            MaxKV = row["额定电压"].ToString(),
                            Poles = row["级数"].ToString(),
                            Amps = double.Parse(current),
                            ResidualCurrent = row["剩余电流动作"].ToString(),
                            CPSCombination = row["组合形式"].ToString(),
                            CPSCharacteristics = row["类别代号"].ToString(),
                            InstallMethod = row["安装方式"].ToString()
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 加载OUVP配置
        /// </summary>
        private static void LoadOUVPConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.OUVPUrl, true);
            var Table = dataSet.Tables["OUVP"];
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    OUVPConfiguration.OUVPComponentInfos.Add(new OUVPComponentInfo()
                    {
                        Model = row["型号"].ToString(),
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
        }
        
        /// <summary>
        /// 加载控制回路配置
        /// </summary>
        private static void LoadSecondaryCircuitConfig()
        {
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.SecondaryCircuitUrl, true);
            var Table = dataSet.Tables["Sheet1"];
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("SmokeExhaustFan", new List<SecondaryCircuitInfo>());//消防排烟风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("MakeupAirFan", new List<SecondaryCircuitInfo>());//消防补风风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("StaircasePressurizationFan", new List<SecondaryCircuitInfo>());//消防加压送风风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("ExhaustFan_Smoke", new List<SecondaryCircuitInfo>());//消防排烟兼平时排风风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("SupplyFan_Smoke", new List<SecondaryCircuitInfo>());//消防补风兼平时送风风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("ExhaustFan", new List<SecondaryCircuitInfo>());//平时排风风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("SupplyFan", new List<SecondaryCircuitInfo>());//平时送风风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("KitchenExhaustFan", new List<SecondaryCircuitInfo>());//厨房排油烟风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("EmergencyFan", new List<SecondaryCircuitInfo>());//事故风机
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("DomesticWaterPump", new List<SecondaryCircuitInfo>());//生活水泵
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("FirePump", new List<SecondaryCircuitInfo>());//消防泵/喷淋泵/消火栓泵
            SecondaryCircuitConfiguration.SecondaryCircuitConfigs.Add("SubmersiblePump", new List<SecondaryCircuitInfo>());//潜水泵
            for (int i = 1; i < Table.Rows.Count; i++)
            {
                var row = Table.Rows[i];
                if (!row[0].ToString().IsNullOrWhiteSpace())
                {
                    SecondaryCircuitInfo info = new SecondaryCircuitInfo()
                    {
                        SecondaryCircuitCode = row["二次回路代号"].ToString(),
                        Description = row["功能描述"].ToString(),
                        Conductor = row["导体根数x每根导体截面积"].ToString(),
                        ConductorCategory = row["导体选择类别"].ToString(),
                    };
                    if (info.SecondaryCircuitCode.Contains("SC-F"))
                    {
                        SecondaryCircuitConfiguration.FireSecondaryCircuitInfos.Add(info);
                    }
                    else
                    {
                        SecondaryCircuitConfiguration.NonFireSecondaryCircuitInfos.Add(info);
                    }
                    if (row["消防排烟风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["SmokeExhaustFan"].Add(info);
                    }
                    if (row["消防补风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["MakeupAirFan"].Add(info);
                    }
                    if (row["消防加压送风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["StaircasePressurizationFan"].Add(info);
                    }
                    if (row["消防排烟兼平时排风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["ExhaustFan_Smoke"].Add(info);
                    }
                    if (row["消防补风兼平时送风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["SupplyFan_Smoke"].Add(info);
                    }
                    if (row["消防泵"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["FirePump"].Add(info);
                    }
                    if (row["平时排风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["ExhaustFan"].Add(info);
                    }
                    if (row["平时送风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["SupplyFan"].Add(info);
                    }
                    if (row["事故风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["EmergencyFan"].Add(info);
                    }
                    if (row["厨房排油烟风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["KitchenExhaustFan"].Add(info);
                    }
                    if (row["生活水泵"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["DomesticWaterPump"].Add(info);
                    }
                    if (row["潜水泵"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitConfigs["SubmersiblePump"].Add(info);
                    }
                }
            }
        }

        /// <summary>
        /// 加载回路创建配置
        /// </summary>
        private static void LoadCircuitConfig()
        {
            //照明回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "正常照明",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "正常照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.CC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "消防备用照明",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "消防备用照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.EmergencyLuminaire,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "单相",
                ConductorType = ConductorType.消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.CC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "机房照明",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "机房照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.CC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "管井照明",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "管井照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "电梯井道照明",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "电梯井道照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "市电监测",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "市电监测",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "正常照明（带接触器）",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "正常照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.CC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "井道照明（带漏电保护）",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "井道照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.WaterproofLights,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "照明回路",
                SubmenuOptions = "夹层检修照明",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "夹层检修照明",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.CC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });

            //插座回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "单相插座",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "插座",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "检修插座",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "检修插座",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "热水器插座",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "热水器",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 9,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "小厨宝",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "小厨宝",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 4,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "烘手机",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "烘手机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 2,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "单相空调插座（立式）",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "空调插座",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "单相空调插座（壁挂）",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "空调插座",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "三相空调插座（立式）",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "空调插座",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "三相空调插座（壁挂）",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "空调插座",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "插座回路",
                SubmenuOptions = "三相插座",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "三相插座",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.FC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 2,
            });

            //配电回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "配电回路",
                SubmenuOptions = "防火卷帘",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "防火卷帘",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.RollerShutter,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = -1,//依照全局配置
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "配电回路",
                SubmenuOptions = "电动挡烟垂壁",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "电动挡烟垂壁",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.ElectricWindow,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "配电回路",
                SubmenuOptions = "电动排烟窗",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "电动排烟窗",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.ElectricWindow,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "配电回路",
                SubmenuOptions = "气体灭火控制器",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "气体灭火控制器",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "单相",
                ConductorType = ConductorType.消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.BC,
                LayingSite2 = LayingSite.CC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });

            //计量回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "计量回路",
                SubmenuOptions = "套内配电箱（单相）",
                CircuitFormOutType = "计量",
                ProtectionSwitchType = "断路器",
                Description = "套内配电箱",
                NodeType = PDSNodeType.DistributionBox,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.ResidentialDistributionPanel,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = -1,//依照全局配置
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "计量回路",
                SubmenuOptions = "套内配电箱（三相）",
                CircuitFormOutType = "计量",
                ProtectionSwitchType = "断路器",
                Description = "套内配电箱",
                NodeType = PDSNodeType.DistributionBox,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.ResidentialDistributionPanel,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = -1,//依照全局配置
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "计量回路",
                SubmenuOptions = "用户配电箱",
                CircuitFormOutType = "计量",
                ProtectionSwitchType = "断路器",
                Description = "用户配电箱",
                NodeType = PDSNodeType.DistributionBox,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.IsolationSwitchPanel,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "计量回路",
                SubmenuOptions = "交流充电桩",
                CircuitFormOutType = "计量",
                ProtectionSwitchType = "组合式RCD",
                Description = "交流充电桩",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.ACCharger,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                ConductorType = ConductorType.非消防配电电线,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = -1,//依照全局配置
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "计量回路",
                SubmenuOptions = "直流非车载充电机",
                CircuitFormOutType = "计量",
                ProtectionSwitchType = "组合式RCD",
                Description = "非车载充电机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.DCCharger,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.CC,
                LayingSite2 = LayingSite.WC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = -1,//依照全局配置
            });

            //单速风机回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "消防排烟风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防排烟风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.SmokeExhaustFan,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 18.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "消防补风风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防补风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.MakeupAirFan,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 18.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "消防加压送风风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防加压送风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.StaircasePressurizationFan,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 18.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "消防排烟兼平时排风风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防排烟兼平时排风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.ExhaustFan_Smoke,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 18.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "消防补风兼平时送风风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防补风兼平时送风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.SupplyFan_Smoke,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 18.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "平时排风风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "平时排风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.ExhaustFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 7.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "平时送风风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "平时送风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.SupplyFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 7.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "事故风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "事故风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.EmergencyFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 7.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "单速风机回路",
                SubmenuOptions = "厨房排油烟风机",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "厨房排油烟风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.KitchenExhaustFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.RS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 7.5,
            });

            //双速风机回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "消防排烟风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防排烟风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.SmokeExhaustFan,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 7.5,
                HighPower = 22,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "消防补风风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防补风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.MakeupAirFan,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 7.5,
                HighPower = 22,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "消防加压送风风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防加压送风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.StaircasePressurizationFan,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 7.5,
                HighPower = 22,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "消防排烟兼平时排风风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防排烟兼平时排风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.ExhaustFan_Smoke,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 7.5,
                HighPower = 22,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "消防补风兼平时送风风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "消防补风兼平时送风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.SupplyFan_Smoke,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 7.5,
                HighPower = 22,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "平时排风风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "平时排风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.ExhaustFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 4,
                HighPower = 12,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "平时送风风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "平时送风风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.SupplyFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 4,
                HighPower = 12,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "事故风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "事故风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.EmergencyFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 4,
                HighPower = 12,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "双速风机回路",
                SubmenuOptions = "厨房排油烟风机",
                CircuitFormOutType = "双速电机Y-Y",
                ProtectionSwitchType = "依照全局配置",
                Description = "厨房排油烟风机",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Fan,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.KitchenExhaustFan,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.RS,
                LayingSite2 = LayingSite.None,
                IsDualPower = true,
                LowPower = 4,
                HighPower = 12,
            });

            //水泵回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "水泵回路",
                SubmenuOptions = "潜水泵",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "潜水泵",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Pump,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.SubmersiblePump,
                FireLoad = true,
                Phase = "三相",
                ConductorType = ConductorType.消防配电分支线路,//配套防水耐火电缆
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 1.5,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "水泵回路",
                SubmenuOptions = "生活水泵",
                CircuitFormOutType = "电动机配电回路",
                ProtectionSwitchType = "依照全局配置",
                Description = "生活水泵",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.Pump,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.DomesticWaterPump,
                FireLoad = false,
                Phase = "三相",
                ConductorType = ConductorType.非消防配电电缆,
                ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                LayingSite1 = LayingSite.WS,
                LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 1.5,
            });

            //备用回路
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "备用回路",
                SubmenuOptions = "单相备用",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "备用",
                NodeType = PDSNodeType.Empty,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                //ConductorType = null,//无
                //ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                //LayingSite1 = LayingSite.WS,
                //LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "备用回路",
                SubmenuOptions = "单相备用（带漏电保护）",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "备用",
                NodeType = PDSNodeType.Empty,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "单相",
                //ConductorType = null,//无
                //ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                //LayingSite1 = LayingSite.WS,
                //LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "备用回路",
                SubmenuOptions = "三相备用",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "断路器",
                Description = "备用",
                NodeType = PDSNodeType.Empty,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                //ConductorType = null,//无
                //ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                //LayingSite1 = LayingSite.WS,
                //LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "备用回路",
                SubmenuOptions = "三相备用（带漏电保护",
                CircuitFormOutType = "常规配电回路",
                ProtectionSwitchType = "组合式RCD",
                Description = "备用",
                NodeType = PDSNodeType.Empty,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.None,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = false,
                Phase = "三相",
                //ConductorType = null,//无
                //ConductorLaying = ConductorLayingPath.ViaCableTrayAndViaConduit,
                //LayingSite1 = LayingSite.WS,
                //LayingSite2 = LayingSite.None,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });

            //集中电源
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "集中电源",
                SubmenuOptions = "应急照明/疏散指示",
                CircuitFormOutType = "消防应急照明回路（WFEL）",
                ProtectionSwitchType = "-",
                Description = "应急照明/疏散指示",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "L",
                //ConductorType = ConductorType.消防控制信号软线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.CC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "集中电源",
                SubmenuOptions = "视觉连续方向标志灯",
                CircuitFormOutType = "消防应急照明回路（WFEL）",
                ProtectionSwitchType = "-",
                Description = "视觉连续方向标志灯",
                NodeType = PDSNodeType.Load,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "L",
                //ConductorType = ConductorType.消防控制信号软线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.FC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
            CircuitConfiguration.CircuitCreatorInfos.Add(new CircuitCreator()
            {
                MenuOptions = "集中电源",
                SubmenuOptions = "备用",
                CircuitFormOutType = "消防应急照明回路（WFEL）",
                ProtectionSwitchType = "-",
                Description = "备用",
                NodeType = PDSNodeType.Empty,
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad,
                LoadTypeCat_2 = ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel,
                LoadTypeCat_3 = ThPDSLoadTypeCat_3.None,
                FireLoad = true,
                Phase = "L",
                //ConductorType = ConductorType.消防控制信号软线,
                ConductorLaying = ConductorLayingPath.ViaConduit,
                LayingSite1 = LayingSite.WC,
                LayingSite2 = LayingSite.FC,
                IsDualPower = false,
                LowPower = 0,
                HighPower = 0,
            });
        }
    }
}
