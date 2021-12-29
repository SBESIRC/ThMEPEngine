using System.Collections.Generic;

namespace ThMEPHVAC.IndoorFanModels
{
    public class IndoorFanLayoutModel
    {
        public IndoorFanLayoutModel() 
        {
            TargetFanInfo = new List<IndoorFanBase>();
            MaxFanType = "";
        }
        /// <summary>
        /// 风机类型
        /// </summary>
        public EnumFanType FanType { get; set; }
        /// <summary>
        /// 风机制冷制热类型
        /// </summary>
        public EnumHotColdType HotColdType { get; set; }
        /// <summary>
        /// 修正系数
        /// </summary>
        public double CorrectionFactor { get; set; }
        /// <summary>
        /// 是否生成送风管
        /// </summary>
        public bool CreateBlastPipe { get; set; }
        /// <summary>
        /// 接回风口形式
        /// </summary>
        public EnumAirReturnType AirReturnType { get; set; }
        /// <summary>
        /// 优先布置方向
        /// </summary>
        public EnumFanDirction FanDirction { get; set; }
        /// <summary>
        /// 最大风机型号自动
        /// </summary>
        public EnumMaxFanNumber MaxFanTypeIsAuto { get; set; }
        /// <summary>
        /// 指定的最大风机型号
        /// </summary>
        public string MaxFanType { get; set; }
        /// <summary>
        /// 选中的工况中对应的风机信息
        /// </summary>
        public List<IndoorFanBase> TargetFanInfo { get; set; }
    }
}
