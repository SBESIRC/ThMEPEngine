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
            LoadMeterConfig();
            LoadMTConfig();
            LoadCPSConfig();
            LoadOUVPConfig();
            LoadSecondaryCircuitConfig();
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
            var dataSet = excelSrevice.ReadExcelToDataSet(ProjectSystemConfiguration.MTSEUrl, true);
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
                new MTComponentInfo(){ Amps = 40, parameter = "10(40)"},
                new MTComponentInfo(){ Amps = 60, parameter = "15(60)"},
                new MTComponentInfo(){ Amps = 80, parameter = "20(80)"},
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
        /// 加载CPS配置
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
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("SmokeExhaustFan", new List<SecondaryCircuitInfo>());//消防排烟风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("MakeupAirFan", new List<SecondaryCircuitInfo>());//消防补风风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("StaircasePressurizationFan", new List<SecondaryCircuitInfo>());//消防加压送风风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("ExhaustFan_Smoke", new List<SecondaryCircuitInfo>());//消防排烟兼平时排风风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("SupplyFan_Smoke", new List<SecondaryCircuitInfo>());//消防补风兼平时送风风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("ExhaustFan", new List<SecondaryCircuitInfo>());//平时排风风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("SupplyFan", new List<SecondaryCircuitInfo>());//平时送风风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("KitchenExhaustFan", new List<SecondaryCircuitInfo>());//厨房排油烟风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("EmergencyFan", new List<SecondaryCircuitInfo>());//事故风机
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("DomesticWaterPump", new List<SecondaryCircuitInfo>());//生活水泵
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("FirePump", new List<SecondaryCircuitInfo>());//消防泵/喷淋泵/消火栓泵
            SecondaryCircuitConfiguration.SecondaryCircuitInfos.Add("SubmersiblePump", new List<SecondaryCircuitInfo>());//潜水泵
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
                    if (row["消防排烟风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["SmokeExhaustFan"].Add(info);
                    }
                    if (row["消防补风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["MakeupAirFan"].Add(info);
                    }
                    if (row["消防加压送风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["StaircasePressurizationFan"].Add(info);
                    }
                    if (row["消防排烟兼平时排风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["ExhaustFan_Smoke"].Add(info);
                    }
                    if (row["消防补风兼平时送风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["SupplyFan_Smoke"].Add(info);
                    }
                    if (row["消防泵"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["FirePump"].Add(info);
                    }
                    if (row["平时排风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["ExhaustFan"].Add(info);
                    }
                    if (row["平时送风风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["SupplyFan"].Add(info);
                    }
                    if (row["事故风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["EmergencyFan"].Add(info);
                    }
                    if (row["厨房排油烟风机"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["KitchenExhaustFan"].Add(info);
                    }
                    if (row["生活水泵"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["DomesticWaterPump"].Add(info);
                    }
                    if (row["潜水泵"].ToString()== "Y")
                    {
                        SecondaryCircuitConfiguration.SecondaryCircuitInfos["SubmersiblePump"].Add(info);
                    }
                }
            }
        }
    }
}
