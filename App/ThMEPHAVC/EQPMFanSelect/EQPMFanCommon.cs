using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.EQPMFanModelEnums;

namespace ThMEPHVAC.EQPMFanSelect
{
    public class EQPMFanCommon
    {
        public static string AXIAL_TYPE_NAME = "轴流";
        public static string AXIAL_BLOCK_NAME = "轴流风机";
        // 图层
        public static string FOUNDATION_LAYER = "H-BASE";
        public static string BLOCK_LAYER_FIRE = "H-FIRE-FBOX";
        public static string BLOCK_LAYER_DUAL = "H-DUAL-FBOX";
        public static string BLOCK_LAYER_EQUP = "H-EQUP-FBOX";

        // 风机块属性
        public static string BLOCK_ATTRIBUTE_EQUIPMENT_SYMBOL = "设备符号";
        public static string BLOCK_ATTRIBUTE_STOREY_AND_NUMBER = "楼层-编号";
        public static string BLOCK_ATTRIBUTE_FAN_USAGE = "风机功能";
        public static string BLOCK_ATTRIBUTE_FAN_VOLUME = "风量";
        public static string BLOCK_ATTRIBUTE_FAN_PRESSURE = "全压";
        public static string BLOCK_ATTRIBUTE_FAN_CHARGE = "电量";
        public static string BLOCK_ATTRIBUTE_FAN_REMARK = "备注";
        public static string BLOCK_ATTRIBUTE_MOUNT_TYPE = "安装方式";
        public static string BLOCK_ATTRIBUTE_FIXED_FREQUENCY = "定频";
        public static string BLOCK_ATTRIBUTE_FIRE_POWER_SUPPLY = "消防电源";
        public static List<FanPrefixDictDataModel> ListFanPrefixDict = new List<FanPrefixDictDataModel>()
        {
            new FanPrefixDictDataModel(){ No = 1, FanUse = EnumScenario.FireSmokeExhaust, Prefix ="ESF", Explain = "" },
            new FanPrefixDictDataModel(){ No = 2, FanUse = EnumScenario.FireAirSupplement, Prefix ="SSF", Explain = "平时风机,自动备注" },
            new FanPrefixDictDataModel(){ No = 3, FanUse = EnumScenario.FirePressurizedAirSupply, Prefix ="SPF", Explain = "" },
            new FanPrefixDictDataModel(){ No = 4, FanUse = EnumScenario.KitchenFumeExhaust, Prefix ="EKF", Explain = "包含燃烧和散热"},
            new FanPrefixDictDataModel(){ No = 5, FanUse = EnumScenario.KitchenFumeExhaustAndAirSupplement,Prefix ="SF", Explain = "包含燃烧和散热" },
            new FanPrefixDictDataModel(){ No = 6, FanUse = EnumScenario.NormalAirSupply, Prefix ="SF", Explain = "平时风机,自动备注"  },
            new FanPrefixDictDataModel(){ No = 7, FanUse = EnumScenario.NormalExhaust, Prefix ="EF", Explain = "平时风机,自动备注" },
            new FanPrefixDictDataModel(){ No = 8, FanUse = EnumScenario.FireSmokeExhaustAndNormalExhaust, Prefix ="E(S)F", Explain = "包含燃烧和散热"},
            new FanPrefixDictDataModel(){ No = 9, FanUse = EnumScenario.FireAirSupplementAndNormalAirSupply, Prefix ="S(S)F", Explain = "包含燃烧和散热" },
            new FanPrefixDictDataModel(){ No = 10, FanUse = EnumScenario.EmergencyExhaust, Prefix ="EF", Explain = "平时风机,自动备注"},
            new FanPrefixDictDataModel(){ No = 11, FanUse = EnumScenario.AccidentAirSupplement, Prefix ="SF", Explain = "平时风机,自动备注" },
            new FanPrefixDictDataModel(){ No = 12, FanUse = EnumScenario.NormalAirSupplyAndAccidentAirSupplement, Prefix ="SF", Explain = "" },
            new FanPrefixDictDataModel(){ No = 13, FanUse = EnumScenario.NormalExhaustAndAccidentExhaust, Prefix ="EF", Explain = "平时风机,自动备注" }
        };
        public static List<SceneResistaCalcModel> ListSceneResistaCalc = new List<SceneResistaCalcModel>()
        {
            new SceneResistaCalcModel(){ No = 1, Scene = EnumScenario.FireSmokeExhaust, Friction = 3,  LocRes = 1.5 ,  Damper =0, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 2, Scene = EnumScenario.FireAirSupplement, Friction = 3,  LocRes = 1.5 ,  Damper =0, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 3, Scene = EnumScenario.FirePressurizedAirSupply, Friction = 3,  LocRes = 1.5 ,  Damper =0, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 4, Scene = EnumScenario.KitchenFumeExhaust, Friction = 2,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },
            new SceneResistaCalcModel(){ No = 5, Scene = EnumScenario.KitchenFumeExhaustAndAirSupplement, Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 6, Scene = EnumScenario.NormalAirSupply, Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },
            new SceneResistaCalcModel(){ No = 7, Scene = EnumScenario.NormalExhaust, Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },
            new SceneResistaCalcModel(){ No = 8, Scene = EnumScenario.FireSmokeExhaustAndNormalExhaust, Friction = 3,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 9, Scene = EnumScenario.FireAirSupplementAndNormalAirSupply, Friction = 3,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },
            new SceneResistaCalcModel(){ No = 10, Scene = EnumScenario.EmergencyExhaust, Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 11, Scene = EnumScenario.AccidentAirSupplement, Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },
            new SceneResistaCalcModel(){ No = 12, Scene = EnumScenario.NormalAirSupplyAndAccidentAirSupplement, Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 13, Scene = EnumScenario.NormalExhaustAndAccidentExhaust, Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  }
        };
        public static List<MotorEfficiency> ListMotorEfficiency = new List<MotorEfficiency>()
        {
            new MotorEfficiency(){ Key ="直连",Value =0.99 },

            new MotorEfficiency(){ Key ="皮带",Value =0.95 }
        };
        public static string FanNameAttr(EnumScenario enumScenario)
        {
            string attr = "";
            var item = ListFanPrefixDict.Where(c => c.FanUse == enumScenario).FirstOrDefault();
            if (null != item)
                attr = item.Prefix;
            return attr;
        }
        public static EnumFanPowerType GetFanPowerType(EnumScenario enumScenario) 
        {
            var res = EnumFanPowerType.OrdinaryPower;
            switch (enumScenario) 
            {
                case EnumScenario.NormalAirSupply:
                case EnumScenario.NormalExhaust:
                case EnumScenario.KitchenFumeExhaust:
                case EnumScenario.KitchenFumeExhaustAndAirSupplement:
                    res = EnumFanPowerType.OrdinaryPower;
                    break;
                case EnumScenario.FireSmokeExhaust:
                case EnumScenario.FirePressurizedAirSupply:
                case EnumScenario.FireAirSupplement:
                case EnumScenario.FireAirSupplementAndNormalAirSupply:
                case EnumScenario.FireSmokeExhaustAndNormalExhaust:
                    res = EnumFanPowerType.FireFightingPower;
                    break;
                case EnumScenario.AccidentAirSupplement:
                case EnumScenario.EmergencyExhaust:
                case EnumScenario.NormalAirSupplyAndAccidentAirSupplement:
                case EnumScenario.NormalExhaustAndAccidentExhaust:
                    res = EnumFanPowerType.EmergencyPower;
                    break;
            }
            return res;
        }

        
        /// <summary>
        /// 属性“设备符号”值
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="installSpace"></param>
        /// <returns></returns>
        public static string Symbol(string scenario, string installSpace)
        {
            if (string.IsNullOrEmpty(installSpace) || installSpace == "未指定子项")
            {
                return scenario;
            }
            else
            {
                return string.Format("{0}-{1}", scenario, installSpace);
            }
        }

        /// <summary>
        /// 属性“楼层-编号”值
        /// </summary>
        public static string StoreyNumber(string storey, string number)
        {
            return string.Format("{0}-{1}", storey, number);
        }

        /// <summary>
        /// 属性"安装方式"值
        /// </summary>
        /// <param name="mount"></param>
        /// <returns></returns>
        public static string Mount(string mount)
        {
            return string.Format("安装方式：{0}", mount);
        }

        /// <summary>
        /// 属性“风量”值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AirVolume(string description)
        {
            return string.Format("风量：{0} cmh", description);
        }

        /// <summary>
        /// 属性“全压”值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string WindResis(string description)
        {
            return string.Format("全压：{0} Pa", description);
        }

        /// <summary>
        /// 属性“电量”值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MotorPower(string description)
        {
            return string.Format("电量：{0} kW", description);
        }

        /// <summary>
        /// 属性“变频”值
        /// </summary>
        /// <param name="control"></param>
        /// <param name="fre"></param>
        /// <returns></returns>
        public static string FixedFrequency(EnumFanControl control)
        {
            string retValue = "";
            switch (control) 
            {
                case EnumFanControl.SingleSpeed:
                    retValue = "定频";
                    break;
                case EnumFanControl.TwoSpeed:
                    retValue = "双速";
                    break;
                case EnumFanControl.Inverters:
                    retValue = "变频";
                    break;
            }
            return retValue;
        }

        /// <summary>
        /// 属性“消防电源”值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FirePower(EnumFanPowerType type)
        {
            if (type == EnumFanPowerType.FireFightingPower)
            {
                return "消防电源";
            }
            else
            {
                return "非消防电源";
            }
        }

        /// <summary>
        /// 规格及型号（轴流风机）
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public static string AXIALModelName(string model, EnumMountingType mount)
        {
            // 寻找最后一个"-"
            string modelName = string.Empty;
            int index = model.LastIndexOf('-');
            if (index == -1)
            {
                modelName = model;
            }
            else
            {
                // "数字+字母"的形式
                string suffix = model.Substring(index + 1, model.Length - index - 1);
                Match match = Regex.Match(suffix, @"^([0-9]*(?:\.[0-9]*)?)([A-Z]+)$");
                if (match.Success)
                {
                    modelName = model.Substring(0, index + 1) + match.Groups[1].Value;
                }
                else
                {
                    modelName = model;
                }
            }
            if (mount == EnumMountingType.Hoisting)
            {
                return string.Format("{0}（{1}）", modelName, "无基础");
            }
            else if (mount == EnumMountingType.FloorSquare)
            {
                return string.Format("{0}{1}", modelName, "方形基础");
            }
            else
            {
                return modelName;
            }
        }

        /// <summary>
        /// 规格及型号（离心风机）
        /// </summary>
        /// <param name="style"></param>
        /// <param name="airflow"></param>
        /// <param name="mount"></param>
        /// <returns></returns>
        public static string HTFCModelName(string style, string airflow, string modelNumber)
        {
            return string.Format("{0} {1}#{2}", style.Substring(0, 2), modelNumber, airflow);
        }

        /// <summary>
        /// 是否为离心风机
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public static bool IsHTFCModelStyle(EnumFanModelType style)
        {
            return style != EnumFanModelType.AxialFlow;
        }

        /// <summary>
        /// 是否为后倾离心风机
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public static bool IsHTFCBackwardModelStyle(EnumFanModelType style)
        {
            var strStyle = CommonUtil.GetEnumDescription(style);
            return IsHTFCModelStyle(style) && strStyle.Contains("后倾");
        }

        /// <summary>
        /// 获取离心风机图块名
        /// </summary>
        /// <param name="style"></param>
        /// <param name="airflow"></param>
        /// <param name="mount"></param>
        /// <returns></returns>
        public static string HTFCBlockName(EnumFanModelType style, EnumFanAirflowDirection airflow, EnumMountingType mount)
        {
            switch (style)
            {
                case EnumFanModelType.ForwardTiltCentrifugation_Out:
                case EnumFanModelType.BackwardTiltCentrifugation_Out:
                    {
                        switch (airflow)
                        {
                            case EnumFanAirflowDirection.StraightInAndStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机外置、直进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机外置、直进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机外置、直进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.SideEntryStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机外置、侧进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机外置、侧进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机外置、侧进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.UpInStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机外置、上进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机外置、上进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机外置、上进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.StraightInAndUpOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机外置、直进上出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机外置、直进上出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机外置、直进上出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.StraightInAndDownOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机外置、直进下出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机外置、直进下出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机外置、直进下出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.DownInStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机外置、下进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机外置、下进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机外置、下进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            default:
                                throw new NotSupportedException();
                        }
                    }
                case EnumFanModelType.BackwardTiltCentrifugation_Inner:
                case EnumFanModelType.ForwardTiltCentrifugation_Inner:
                    {
                        switch (airflow)
                        {
                            case EnumFanAirflowDirection.StraightInAndStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机内置、直进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机内置、直进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机内置、直进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.SideEntryStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机内置、侧进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机内置、侧进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机内置、侧进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.UpInStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机内置、上进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机内置、上进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机内置、上进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.StraightInAndUpOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机内置、直进上出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机内置、直进上出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机内置、直进上出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.StraightInAndDownOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机内置、直进下出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机内置、直进下出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机内置、直进下出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case EnumFanAirflowDirection.DownInStraightOut:
                                {
                                    switch (mount)
                                    {
                                        case EnumMountingType.FloorBar:
                                            return "离心风机(电机内置、下进直出、有基础)";
                                        case EnumMountingType.FloorSquare:
                                            return "离心风机(电机内置、下进直出、有基础2)";
                                        case EnumMountingType.Hoisting:
                                            return "离心风机(电机内置、下进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            default:
                                throw new NotSupportedException();
                        }
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
