using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    public class ExhaustCalcModel : IFanModel
    {

        /// <summary>
        /// 类型
        /// </summary>
        public string ExhaustCalcType { get; set; }


        ///第一部分：Basic
        /// <summary>
        /// 空间类型
        /// </summary>
        public string SpatialTypes { get; set; }

        /// <summary>
        /// 有无喷淋
        /// </summary>
        public bool IsSpray { get; set; }

        /// <summary>
        /// 空间净高
        /// </summary>
        public string SpaceHeight { get; set; }

        /// <summary>
        /// 建筑面积
        /// </summary>
        public string CoveredArea { get; set; }


        /// <summary>
        /// 最小风量
        /// </summary>
        public string MinAirVolume { get; set; }

        /// <summary>
        /// 单位风量
        /// </summary>
        public string UnitVolume { get; set; }

        ///第二部分：风量计算
        /// <summary>
        /// 热释放速率
        /// </summary>
        public string HeatReleaseRate { get; set; }


        /// <summary>
        /// 烟羽流选择
        /// </summary>
        public string PlumeSelection { get; set; }

        /// <summary>
        /// 最高层高
        /// </summary>
        public string Axial_HighestHeight { get; set; }

        /// <summary>
        /// 垂壁到地
        /// </summary>
        public string Axial_HangingWallGround { get; set; }

        /// <summary>
        /// 燃料到地坪
        /// </summary>
        public string Axial_FuelFloor { get; set; }

        /// <summary>
        /// 计算风量
        /// </summary>
        public string Axial_CalcAirVolum { get; set; }

        /// <summary>
        /// 燃料到阳台
        /// </summary>
        public string Spill_FuelBalcony { get; set; }

        /// <summary>
        /// 阳台到烟底
        /// </summary>
        public string Spill_BalconySmokeBottom { get; set; }

        /// <summary>
        /// 火源开口
        /// </summary>
        public string Spill_FireOpening { get; set; }

        /// <summary>
        /// 开口至阳台
        /// </summary>
        public string Spill_OpenBalcony { get; set; }

        /// <summary>
        /// 计算风量
        /// </summary>
        public string Spill_CalcAirVolum { get; set; }

        /// <summary>
        /// 窗口面积
        /// </summary>
        public string Window_WindowArea { get; set; }

        /// <summary>
        /// 窗口高度
        /// </summary>
        public string Window_WindowHeight { get; set; }

        /// <summary>
        /// 开口顶至烟底
        /// </summary>
        public string Window_SmokeBottom { get; set; }

        /// <summary>
        /// 计算风量
        /// </summary>
        public string Window_CalcAirVolum { get; set; }

        /// <summary>
        /// 最终选择的计算风量
        /// </summary>
        public string Final_CalcAirVolum { get; set; }

        ///第三部分：其他
        /// <summary>
        /// 风口-长
        /// </summary>
        public string SmokeLength { get; set; }

        /// <summary>
        /// 风口-宽
        /// </summary>
        public string SmokeWidth { get; set; }

        /// <summary>
        /// 当量直径
        /// </summary>
        public string SmokeDiameter { get; set; }

        /// <summary>
        /// 排烟位置系数-值
        /// </summary>
        public string SmokeFactorValue { get; set; }

        /// <summary>
        /// 排烟位置系数-选项
        /// </summary>
        public string SmokeFactorOption { get; set; }

        /// <summary>
        /// 风口下烟层厚度
        /// </summary>
        public string SmokeThickness { get; set; }

        /// <summary>
        /// 最大允许排烟量
        /// </summary>
        public string MaxSmokeExtraction { get; set; }
    }
}
