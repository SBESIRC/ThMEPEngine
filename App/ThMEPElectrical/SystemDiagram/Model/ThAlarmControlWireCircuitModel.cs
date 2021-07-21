using System;
using QuickGraph;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Engine;

namespace ThMEPElectrical.SystemDiagram.Model
{
    public class ThAlarmControlWireCircuitModel
    {
        public string WireCircuitName { get; set; } = string.Empty;

        public AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>> Graph { get; set; }

        public DataSummary Data { get; set; }

        /// <summary>
        /// 线路编号
        /// </summary>
        public int WireCircuitNo { get; set; } = 0;

        /// <summary>
        /// 合并线路名称
        /// </summary>
        public string MulitWireCircuitName { get; set; }

        /// <summary>
        /// 块权重计数
        /// </summary>
        public int BlockCount { get; set; }

        /// <summary>
        /// 线路编号坐标
        /// </summary>
        public Point3d TextPoint { get; set; }

        /// <summary>
        /// 是否绘画线路
        /// </summary>
        public bool DrawWireCircuit { get; set; } = true;

        /// <summary>
        /// 是否绘画线路编号
        /// </summary>
        public bool DrawWireCircuitText { get; set; } = false;

        public void FillingData(Dictionary<Entity, List<KeyValuePair<string, string>>> GlobleBlockAttInfoDic, bool IsJF = false)
        {
            ThBlockNumStatistics BlockDataReturn = new ThBlockNumStatistics();
            var VerticesData = this.Graph.Vertices.Select(o => o.VertexElement).Where(o => o is BlockReference).Cast<BlockReference>().ToList();
            ThBlockConfigModel.BlockConfig.ForEach(o =>
            {
                switch (o.StatisticMode)
                {
                    case StatisticType.BlockName:
                        {
                            if (ThAutoFireAlarmSystemCommon.NotInAlarmControlWireCircuitBlockNames.Contains(o.BlockName))
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = 0;//寻路算法不统计不属于自动报警控制总线的块
                            }
                            else
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = VerticesData.Count(x => x.Name == o.BlockName);
                                if (o.HasAlias)
                                {
                                    BlockDataReturn.BlockStatistics[o.UniqueName] += VerticesData.Count(x => o.AliasList.Contains(x.Name));
                                }
                            }
                            break;
                        }
                    case StatisticType.Attributes:
                        {
                            BlockDataReturn.BlockStatistics[o.UniqueName] = VerticesData.Count(x => GlobleBlockAttInfoDic.ContainsKey(x) && (GlobleBlockAttInfoDic.First(b => b.Key.Equals(x))).Value.Count(y => o.StatisticAttNameValues.ContainsKey(y.Key) && o.StatisticAttNameValues[y.Key].Contains(y.Value)) > 0);
                            break;
                        }
                    case StatisticType.RelyOthers:
                        {
                            break;
                        }
                    case StatisticType.NoStatisticsRequired:
                        {
                            break;
                        }
                    case StatisticType.Room:
                        {
                            break;
                        }
                    case StatisticType.NeedSpecialTreatment:
                        {
                            //如果是大屋面
                            if (IsJF && o.UniqueName == "消防水箱")
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = VerticesData.Count(x => x.Name == o.BlockName);
                            }
                            //其他正常楼层
                            else if (!IsJF && o.UniqueName == "消防水池")
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = VerticesData.Count(x => x.Name == o.BlockName);
                            }
                            break;
                        }
                    default:
                        break;
                }
            });
            #region 最后统计需依赖其他模块的计数规则
            //#5
            ThBlockConfigModel.BlockConfig.Where(b => b.StatisticMode == StatisticType.RelyOthers).ForEach(o =>
            {
                int FindCount = 0;
                o.RelyBlockUniqueNames.ForEach(name =>
                {
                    FindCount += BlockDataReturn.BlockStatistics[name] * ThBlockConfigModel.BlockConfig.First(x => x.UniqueName == name).CoefficientOfExpansion;//计数*权重
                });
                BlockDataReturn.BlockStatistics[o.UniqueName] = (int)Math.Ceiling((double)FindCount / o.DependentStatisticalRule);//向上缺省
            });
            //与读取到的[消防水箱],[灭火系统压力开关]同一分区，默认数量为2
            if (BlockDataReturn.BlockStatistics["消防水池"] > 0 && BlockDataReturn.BlockStatistics["灭火系统压力开关"] > 0)
            {
                BlockDataReturn.BlockStatistics["消火栓泵"] = Math.Max(BlockDataReturn.BlockStatistics["消火栓泵"], 2);
                BlockDataReturn.BlockStatistics["喷淋泵"] = Math.Max(BlockDataReturn.BlockStatistics["喷淋泵"], 2);
            }
            //有[消火栓泵],[喷淋泵]，默认一定至少有一个[灭火系统压力开关]
            if (BlockDataReturn.BlockStatistics["消火栓泵"] > 0 || BlockDataReturn.BlockStatistics["喷淋泵"] > 0)
            {
                BlockDataReturn.BlockStatistics["灭火系统压力开关"] = Math.Max(BlockDataReturn.BlockStatistics["灭火系统压力开关"], 1);
            }
            //回路的短路隔离器模块需要按照真实数量计数，不可再按照逻辑去划分,但是业务需求，每个回路短路隔离器至少一个
            BlockDataReturn.BlockStatistics["短路隔离器"] = Math.Max(1, VerticesData.Count(x => x.Name == "E-BFAS540"));
            #endregion
            //填充到Data
            this.Data = new DataSummary()
            {
                BlockData = BlockDataReturn
            };

            int blockCount = 0;
            ThAutoFireAlarmSystemCommon.AlarmControlWireCircuitBlocks.ForEach(name =>
            {
                blockCount += Data.BlockData.BlockStatistics[name] * ThBlockConfigModel.BlockConfig.First(x => x.UniqueName == name).CoefficientOfExpansion;//计数*权重
            });
            this.BlockCount = blockCount;
        }

        public static ThAlarmControlWireCircuitModel operator +(ThAlarmControlWireCircuitModel x, ThAlarmControlWireCircuitModel y)
        {
            ThAlarmControlWireCircuitModel newWireCircuitModel = new ThAlarmControlWireCircuitModel()
            {
                WireCircuitName = x.WireCircuitName,
                WireCircuitNo=x.WireCircuitNo,
                TextPoint = x.TextPoint.Equals(Point3d.Origin) ? y.TextPoint : x.TextPoint,
                Data = x.Data + y.Data,
                BlockCount = x.BlockCount + y.BlockCount,
                Graph = x.Graph,
            };
            newWireCircuitModel.Data.BlockData.BlockStatistics["短路隔离器"] = x.Data.BlockData.BlockStatistics["短路隔离器"] + y.Data.BlockData.BlockStatistics["短路隔离器"];
            newWireCircuitModel.Graph.AddVertexRange(y.Graph.Vertices.Select(v => { v.IsStartVertexOfGraph = false; return v; }));
            newWireCircuitModel.Graph.AddEdgeRange(y.Graph.Edges);
            return newWireCircuitModel;
        }
    }
}
