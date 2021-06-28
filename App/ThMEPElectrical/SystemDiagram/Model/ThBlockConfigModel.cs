using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SystemDiagram.Service;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 块配置白名单
    /// 所有的块都在此配置，如新增块等操作
    /// </summary>
    public static class ThBlockConfigModel
    {
        public static List<ThBlockModel> BlockConfig;
        public static void Init()
        {
            if (!BlockConfig.IsNull())
            {
                //重新加载用户配置
                var SpecialBlock = BlockConfig.Where(o => o.StatisticMode == StatisticType.RelyOthers);
                SpecialBlock.First(o => o.UniqueName == "短路隔离器").DependentStatisticalRule = FireCompartmentParameter.ShortCircuitIsolatorCount;
                SpecialBlock.First(o => o.UniqueName == "消防广播火栓强制启动模块").DependentStatisticalRule = FireCompartmentParameter.FireBroadcastingCount;
                return;
            }
            BlockConfig = new List<ThBlockModel>();
            #region #1
            #endregion
            #region #2
            #endregion
            #region #3
            #endregion
            #region #4
            #endregion
            #region #5
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "短路隔离器",
                BlockName = "E-BFAS540",
                BlockAliasName = "E-BFAS540",
                BlockNameRemark = "短路隔离器",
                Index = 5,
                Position = new Point3d(700, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1050, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "SI" } },
                StatisticMode = StatisticType.RelyOthers,
                RelyBlockUniqueNames = ThAutoFireAlarmSystemCommon.AlarmControlWireCircuitBlocks,
                DependentStatisticalRule = FireCompartmentParameter.ShortCircuitIsolatorCount
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "分区声光报警器",
                BlockName = "E-BFAS520",
                BlockAliasName = "E-BFAS520_分区声光报警器",
                BlockNameRemark = "分区声光报警器",
                Index = 5,
                Position = new Point3d(1500, 800, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "I/O" } },
                StatisticMode=StatisticType.NoStatisticsRequired,
                DefaultQuantity=1
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "消防广播火栓强制启动模块",
                BlockName = "E-BFAS520",
                BlockAliasName = "E-BFAS520_消防广播火栓强制启动模块",
                BlockNameRemark = "消防广播火栓强制启动模块",
                Index = 5,
                Position = new Point3d(2300, 1150, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(2650, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "I/O" } },
                StatisticMode = StatisticType.RelyOthers,
                RelyBlockUniqueNames = new List<string>() { "火灾应急广播扬声器-2","火灾应急广播扬声器-3","火灾应急广播扬声器-4" },
                DependentStatisticalRule = FireCompartmentParameter.FireBroadcastingCount
            });
            #endregion
            #region #6
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "区域显示器/火灾显示盘",
                BlockName = "E-BFAS030",
                BlockAliasName = "E-BFAS030",
                BlockNameRemark = "区域显示器/火灾显示盘",
                Index = 6,
                CanHidden = true,
                Position = new Point3d(1500, 450, 0),
                ShowQuantity=true,
                QuantityPosition=new Point3d(1850,450,0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "D" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "楼层或回路重复显示屏",
                BlockName = "E-BFAS031",
                BlockAliasName = "E-BFAS031",
                BlockNameRemark = "楼层或回路重复显示屏",
                Index = 6,
                CanHidden = true,
                Position = new Point3d(1500, 450, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 450, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "FI" } }
            });
            #endregion
            #region #7
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "手动火灾报警按钮(带消防电话插座)",
                BlockName = "E-BFAS212",
                BlockAliasName = "E-BFAS212",
                BlockNameRemark = "手动火灾报警按钮(带消防电话插座)",
                Index = 7,
                CanHidden = false,
                Position = new Point3d(1500, 1500, 0),
                ShowQuantity=true,
                QuantityPosition=new Point3d(1850,1150,0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "火灾报警电话",
                BlockName = "E-BFAS220",
                BlockAliasName = "E-BFAS220",
                BlockNameRemark = "火灾报警电话",
                Index = 7,
                CanHidden = false,
                Position = new Point3d(2250, 800, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "火灾声光警报器",
                BlockName = "E-BFAS330",
                BlockAliasName = "E-BFAS330",
                BlockNameRemark = "火灾声光警报器",
                Index = 7,
                CanHidden = false,
                Position = new Point3d(1500, 800, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 450, 0)
            });
            #endregion
            #region #8
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "火灾应急广播扬声器-2",
                BlockName = "E-BFAS410-2",
                BlockAliasName = "E-BFAS410-2",
                BlockNameRemark = "火灾应急广播扬声器",
                Index = 8,
                Position = new Point3d(1500, 800, 0),
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 450, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "C" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "火灾应急广播扬声器-3",
                BlockName = "E-BFAS410-3",
                BlockAliasName = "E-BFAS410-3",
                BlockNameRemark = "火灾应急广播扬声器",
                Index = 8,
                Position = new Point3d(750, 800, 0),
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 450, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "R" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "火灾应急广播扬声器-4",
                BlockName = "E-BFAS410-4",
                BlockAliasName = "E-BFAS410-4",
                BlockNameRemark = "火灾应急广播扬声器",
                Index = 8,
                Position = new Point3d(2250, 800, 0),
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 450, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "W" } }
            });
            #endregion
            #region #9
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "感烟火灾探测器",
                BlockName = "E-BFAS110",
                BlockAliasName = "E-BFAS110",
                BlockNameRemark = "感烟火灾探测器",
                Index = 9,
                CanHidden = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 1150, 0),
                Position = new Point3d(750, 1500, 0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "感温火灾探测器",
                BlockName = "E-BFAS120",
                BlockAliasName = "E-BFAS120",
                BlockNameRemark = "感温火灾探测器",
                Index = 9,
                CanHidden = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0),
                Position = new Point3d(2250, 1500, 0)
            });
            #endregion
            #region #10
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "红外光束感烟火灾探测器发射器",
                BlockName = "E-BFAS112",
                BlockAliasName = "E-BFAS112",
                BlockNameRemark = "红外光束感烟火灾探测器发射器",
                Index = 10,
                CanHidden = true,
                Position = new Point3d(1000, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1350, 1150, 0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "红外光束感烟火灾探测器接收器",
                BlockName = "E-BFAS113",
                BlockAliasName = "E-BFAS113",
                BlockNameRemark = "红外光束感烟火灾探测器接收器",
                Index = 10,
                CanHidden = true,
                Position = new Point3d(2000, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(2350, 1150, 0)
            });
            #endregion
            #region #11
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "强电间总线控制模块",
                BlockName = "E-BFAS520",
                BlockNameRemark = "强电间非消防电源切除",
                Index = 11,
                Position = new Point3d(1500, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "I/O" } },
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "BOX", new List<string>() { "FXDY" } } },
                CanHidden = true,
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0)
            });
            #endregion
            #region #12
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "弱电间总线控制模块",
                BlockName = "E-BFAS520",
                BlockNameRemark = "弱电间弱电系统消防联动",
                Index = 12,
                Position = new Point3d(1500, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "I/O" } },
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "BOX", new List<string>() { "MJ" } } },
                CanHidden =true,
                ShowText=true,
                TextPosition = new Point3d(1500, 450, 0)
            });
            #endregion
            #region #13
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "防火卷帘模块",
                BlockName = "E-BFAS011",
                BlockNameRemark = "防火卷帘",
                Index = 13,
                Position = new Point3d(750, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "I/O" } },
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "BOX", new List<string>() { "RS" } } },
                CanHidden = true,
                ShowText = true,
                TextPosition = new Point3d(750, 450, 0),
                CoefficientOfExpansion=2
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "电梯模块",
                BlockName = "E-BFAS011",
                BlockNameRemark = "电梯",
                Index = 13,
                Position = new Point3d(2250, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "I/O" } },
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "BOX", new List<string>() { "DT", "L" } } },
                CanHidden = true,
                ShowText = true,
                TextPosition = new Point3d(2250, 450, 0),
                CoefficientOfExpansion = 5
            });
            #endregion
            #region #14
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "70℃防火阀+输入模块",
                BlockName = "E-BFAS711",
                BlockAliasName = "E-BFAS711",
                BlockNameRemark = "70℃防火阀+输入模块",
                Index = 14,
                Position = new Point3d(750, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 1150, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "70℃" } },
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "F", new List<string>() { "70℃" } } },
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "150℃防火阀+输入模块",
                BlockName = "E-BFAS713",
                BlockAliasName = "E-BFAS712",
                BlockNameRemark = "150℃防火阀+输入模块",
                Index = 14,
                Position = new Point3d(1500, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 1150, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "150℃" } },
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "F", new List<string>() { "150℃" } } },
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "280℃防火阀+输入模块",
                BlockName = "E-BFAS712",
                BlockAliasName = "E-BFAS713",
                BlockNameRemark = "280℃防火阀+输入模块",
                Index = 14,
                Position = new Point3d(2250, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "280℃" } },
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "F", new List<string>() { "280℃" } } }
            });
            #endregion
            #region #15
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "电动防火阀",
                BlockName = "E-BFAS730",
                BlockNameRemark = "电动防火阀",
                Index = 15,
                Position = new Point3d(1500, 1200, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "E" } },
                StatisticMode = StatisticType.BlockName,
                CanHidden = true,
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0),
                HasMultipleBlocks = true,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "电动防火阀_1",
                        BlockName = "E-BFAS520",
                        BlockNameRemark = "电动防火阀_1",
                        Index = 15,
                        Position = new Point3d(1500, 1500, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "F", "I/O" } }
                    }
                }
            });
            #endregion
            #region #16
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "防排抽烟机",
                BlockName = "E-BFAS522",
                BlockAliasName = "E-BFAS522_16",
                BlockNameRemark = "防排抽烟机",
                Index = 16,
                Position = new Point3d(1100, 1200, 0),
                CanHidden = true,
                ShowAtt = false,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 850, 0),
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0),
                StatisticMode=StatisticType.Attributes,
                StatisticAttNameValues=new Dictionary<string, List<string>>() { { "BOX", new List<string>() { "FJ","ESF","SPF","SSF" } } },
                HasMultipleBlocks = true,
                CoefficientOfExpansion = 5,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "防排抽烟机_1",
                        BlockName = "E-BDB004",
                        BlockAliasName = "E-BDB004",
                        BlockNameRemark = "防排抽烟机_1",
                        Index = 16,
                        Position = new Point3d(1500, 1200, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "BOX", "APE" } }
                    },
                    new ThBlockModel()
                    {
                        UniqueName = "防排抽烟机_2",
                        BlockName = "E-BFAS011",
                        BlockAliasName = "E-BFAS011",
                        BlockNameRemark = "防排抽烟机_2",
                        Index = 16,
                        Position = new Point3d(1500, 1500, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "F", "I/O" } }
                    }
                }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "旁通阀",
                BlockName = "E-BFAS621-4",
                BlockNameRemark = "旁通阀",
                Index = 16,
                Position = new Point3d(2250, 1200, 0),
                CanHidden = true,
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "M" } },
                ShowQuantity = true,
                QuantityPosition = new Point3d(2500, 850, 0),
                ShowText = true,
                TextPosition = new Point3d(2250, 450, 0),
                StatisticMode = StatisticType.BlockName,
                HasMultipleBlocks = true,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "旁通阀_1",
                        BlockName = "E-BFAS510",
                        BlockNameRemark = "旁通阀_1", 
                        Index = 16,
                        Position = new Point3d(2250, 1500, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "F", "I" } }
                    },
                }
            });
            #endregion
            #region #17
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "消火栓按钮",
                BlockName = "E-BFAS610",
                BlockNameRemark = "消火栓按钮",
                Index = 17,
                CanHidden=true,
                Position = new Point3d(750, 1500, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 1150, 0),
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "灭火系统流量开关",
                BlockName = "E-BFAS622-2",
                BlockNameRemark = "灭火系统流量开关",
                Index = 17,
                Position = new Point3d(2250, 1500, 0),
                CanHidden = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0),
            });
            #endregion
            #region #18
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "水流指示器",
                BlockName = "E-BFAS622",
                BlockNameRemark = "水流指示器",
                Index = 18,
                Position = new Point3d(900, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "S", "L" } },
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 1150, 0),
                HasMultipleBlocks = true,
                CoefficientOfExpansion=2,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "水流指示器_信号阀",
                        BlockName = "E-BFAS621-2",
                        BlockNameRemark = "水流指示器_信号阀",
                        Index = 18,
                        Position = new Point3d(600, 1500, 0),
                    },
                }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "灭火系统压力开关",
                BlockName = "E-BFAS620",
                BlockNameRemark = "灭火系统压力开关",
                Index = 18,
                Position = new Point3d(2400, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "P" } },
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0),
                HasMultipleBlocks = true,
                CoefficientOfExpansion = 2,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "灭火系统压力开关_信号阀",
                        BlockName = "E-BFAS621-2",
                        BlockNameRemark = "灭火系统压力开关_信号阀",
                        Index = 18,
                        Position = new Point3d(2100, 1500, 0),
                    },
                }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "消防水箱",
                BlockName = "E-BFAS630-2",
                BlockNameRemark = "消防水箱",
                Index = 18,
                Position = new Point3d(1500, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "F" } },
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 850, 0),
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0),
                StatisticMode=StatisticType.NeedSpecialTreatment,
                HasMultipleBlocks = true,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "消防水箱_水池液位传感器",
                        BlockName = "E-BFAS-630-5",
                        BlockNameRemark = "消防水箱_水池液位传感器",
                        Index = 18,
                        Position = new Point3d(1500, 2350, 0),
                        ShowAtt = true,
                        attNameValues = new Dictionary<string, string>() { { "F", "F" } ,{ "LT", "LT" }},
                    }
                }
            });
            #endregion
            #region #19
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "消火栓泵",
                BlockName = "E-BFAS522",
                BlockNameRemark = "消火栓泵",
                Index = 19,
                Position = new Point3d(1100, 1200, 0),
                CanHidden = true,
                ShowAtt = false,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 850, 0),
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0),
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "BOX", new List<string>() { "XHSB", "XFB", "XHS" } } },
                HasMultipleBlocks = true,
                CoefficientOfExpansion = 5,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "消火栓泵_1",
                        BlockName = "E-BDB004",
                        BlockAliasName = "E-BDB004",
                        BlockNameRemark = "消火栓泵_1",
                        Index = 19,
                        Position = new Point3d(1500, 1200, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "BOX", "APE" } }
                    },
                    new ThBlockModel()
                    {
                        UniqueName = "防排抽烟机_2",
                        BlockName = "E-BFAS011",
                        BlockAliasName = "E-BFAS011",
                        BlockNameRemark = "防排抽烟机_2",
                        Index = 19,
                        Position = new Point3d(1500, 1500, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "F", "I/O" } }
                    },
                    new ThBlockModel()
                    {
                        UniqueName = "消火栓泵直接启动信号线",
                        BlockName = "消火栓泵直接启动信号线",
                        BlockNameRemark = "消火栓泵直接启动信号线",
                        Index = 19,
                        Position = new Point3d(0, 0, 0),
                    }
                }
            });
            #endregion
            #region #20
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "喷淋泵",
                BlockName = "E-BFAS522",
                BlockNameRemark = "喷淋泵",
                Index = 20,
                Position = new Point3d(1100, 1200, 0),
                CanHidden = true,
                ShowAtt = false,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 850, 0),
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0),
                StatisticMode = StatisticType.Attributes,
                StatisticAttNameValues = new Dictionary<string, List<string>>() { { "BOX", new List<string>() { "PLB", "PL" } } },
                HasMultipleBlocks = true,
                CoefficientOfExpansion = 5,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "喷淋泵_1",
                        BlockName = "E-BDB004",
                        BlockNameRemark = "喷淋泵_1",
                        Index = 20,
                        Position = new Point3d(1500, 1200, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "BOX", "APE" } }
                    },
                    new ThBlockModel()
                    {
                        UniqueName = "喷淋泵_2",
                        BlockName = "E-BFAS011",
                        BlockNameRemark = "喷淋泵_2",
                        Index = 20,
                        Position = new Point3d(1500, 1500, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "F", "I/O" } }
                    },
                    new ThBlockModel()
                    {
                        UniqueName = "喷淋泵直接启动信号线",
                        BlockName = "喷淋泵直接启动信号线",
                        BlockNameRemark = "喷淋泵直接启动信号线",
                        Index = 20,
                        Position = new Point3d(0, 0, 0),
                    }
                }
            });
            #endregion
            #region #21
            BlockConfig.Add(new ThBlockModel()
            {
                UniqueName = "消防水池",
                BlockName = "E-BFAS630-2",
                BlockNameRemark = "消防水池",
                Index = 21,
                Position = new Point3d(1500, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "F" } },
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 850, 0),
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0),
                StatisticMode = StatisticType.NeedSpecialTreatment,
                HasMultipleBlocks = true,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        UniqueName = "消防水池_水池液位传感器",
                        BlockName = "E-BFAS-630-5",
                        BlockNameRemark = "消防水池_水池液位传感器",
                        Index = 21,
                        Position = new Point3d(1500, 2350, 0),
                        ShowAtt = true,
                        attNameValues = new Dictionary<string, string>() { { "F", "F" } ,{ "LT", "LT" }},
                    }
                }
            });
            #endregion
        }
    }
}
