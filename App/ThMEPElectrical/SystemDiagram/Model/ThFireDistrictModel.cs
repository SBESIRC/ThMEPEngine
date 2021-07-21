using System;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 防火分区
    /// </summary>
    public class ThFireDistrictModel
    {
        public string FireDistrictName { get; set; }

        public DataSummary Data { get; set; }

        /// <summary>
        /// 防火分区PolyLine
        /// </summary>
        public Entity FireDistrictBoundary { get; set; }

        /// <summary>
        /// 防火分区编号
        /// </summary>
        public int FireDistrictNo { get; set; } = 0;

        /// <summary>
        /// 防火分区编号坐标
        /// </summary>
        public Point3d TextPoint { get; set; }

        /// <summary>
        /// 是否绘画防火分区编号
        /// </summary>
        public bool DrawFireDistrictNameText { get; set; } = false;

        /// <summary>
        /// 是否绘画该防火分区
        /// </summary>
        public bool DrawFireDistrict { get; set; } = true;

        /// <summary>
        /// 所拥有的线路
        /// </summary>
        public List<ThAlarmControlWireCircuitModel> WireCircuits { get; set; }

        public List<BlockReference> NotInAlarmControlWireCircuitData { get; set; }

        /// <summary>
        /// 初始化楼层（无防火分区）
        /// </summary>
        /// <param name="FireDistrictBlockReference">楼层块</param>
        public void InitFireDistrict(string floorNumber, BlockReference FireDistrictBlockReference)
        {
            this.FireDistrictNo = 1;
            this.FireDistrictBoundary = new Polyline() { Closed = true };
            this.WireCircuits = new List<ThAlarmControlWireCircuitModel>();
            (this.FireDistrictBoundary as Polyline).CreatePolyline(FireDistrictBlockReference.GeometricExtents.ToRectangle().Vertices());
        }

        /// <summary>
        /// 初始化楼层(防火分区)
        /// </summary>
        /// <param name="FireDistrict"></param>
        internal void InitFireDistrict(string floorNumber, ThFireCompartment FireDistrict)
        {
            this.FireDistrictBoundary = FireDistrict.Boundary;
            this.WireCircuits = new List<ThAlarmControlWireCircuitModel>();
            if (string.IsNullOrWhiteSpace(FireDistrict.Number))
            {
                this.DrawFireDistrictNameText = true;
                if (this.FireDistrictBoundary is Polyline polyline)
                {
                    this.TextPoint = polyline.GetMaximumInscribedCircleCenter();
                }
                if (this.FireDistrictBoundary is MPolygon Mpolygon)
                {
                    this.TextPoint = Mpolygon.GetMaximumInscribedCircleCenter();
                }
            }
            else
            {
                string[] FireDistrictInfo = FireDistrict.Number.Split('-');
                if (FireDistrictInfo[0] == floorNumber || (FireDistrictInfo[0].Contains('F') && floorNumber.Contains('F') && FireDistrictInfo[0].Replace("F", "") == floorNumber.Replace("F", "")))//防火分区和楼层一致才能说明该防火分区属于本楼层
                    this.FireDistrictNo = int.Parse(FireDistrictInfo[1]);
                else
                {
                    this.FireDistrictNo = -1;//该防火分区不属于本楼层，打个标记
                    if (FireDistrictInfo[0] == "*")
                        this.FireDistrictNo = -2;//是本系统手动生成的防火分区，需提示用户
                }
                this.FireDistrictName = FireDistrict.Number;
            }
        }
    }

    /// <summary>
    /// 数据层
    /// </summary>
    public class DataSummary
    {
        public ThBlockNumStatistics BlockData { get; set; }
        public static DataSummary operator +(DataSummary x, DataSummary y)
        {
            DataSummary DataSummaryReturn = new DataSummary() { BlockData = new ThBlockNumStatistics() };
            ThBlockConfigModel.BlockConfig.ForEach(o => DataSummaryReturn.BlockData.BlockStatistics[o.UniqueName] = x.BlockData.BlockStatistics[o.UniqueName] + y.BlockData.BlockStatistics[o.UniqueName]);
            #region 最后统计需依赖其他模块的计数规则
            //#5
            ThBlockConfigModel.BlockConfig.Where(b => b.StatisticMode == StatisticType.RelyOthers).ForEach(o =>
            {
                int FindCount = 0;
                o.RelyBlockUniqueNames.ForEach(name =>
                {
                    FindCount += DataSummaryReturn.BlockData.BlockStatistics[name] * ThBlockConfigModel.BlockConfig.First(c => c.UniqueName == name).CoefficientOfExpansion;//计数*权重
                });
                DataSummaryReturn.BlockData.BlockStatistics[o.UniqueName] = (int)Math.Ceiling((double)FindCount / o.DependentStatisticalRule);//向上缺省
            });
            //与读取到的[消防水箱],[灭火系统压力开关]同一分区，默认数量为2
            if (DataSummaryReturn.BlockData.BlockStatistics["消防水池"] > 0 && DataSummaryReturn.BlockData.BlockStatistics["灭火系统压力开关"] > 0)
            {
                DataSummaryReturn.BlockData.BlockStatistics["消火栓泵"] = Math.Max(DataSummaryReturn.BlockData.BlockStatistics["消火栓泵"], 2);
                DataSummaryReturn.BlockData.BlockStatistics["喷淋泵"] = Math.Max(DataSummaryReturn.BlockData.BlockStatistics["喷淋泵"], 2);
            }
            //有[消火栓泵],[喷淋泵]，默认一定至少有一个[灭火系统压力开关]
            if (DataSummaryReturn.BlockData.BlockStatistics["消火栓泵"] > 0 || DataSummaryReturn.BlockData.BlockStatistics["喷淋泵"] > 0)
            {
                DataSummaryReturn.BlockData.BlockStatistics["灭火系统压力开关"] = Math.Max(DataSummaryReturn.BlockData.BlockStatistics["灭火系统压力开关"], 1);
            }
            //设置默认值
            ThBlockConfigModel.BlockConfig.Where(b => b.DefaultQuantity != 0).ForEach(o =>
            {
                DataSummaryReturn.BlockData.BlockStatistics[o.UniqueName] = o.DefaultQuantity;
            });
            #endregion
            return DataSummaryReturn;
        }
    }
}
