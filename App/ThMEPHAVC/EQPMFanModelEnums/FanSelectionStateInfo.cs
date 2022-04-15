using System.Collections.Generic;

namespace ThMEPHVAC.EQPMFanModelEnums
{
    public enum EnumFanSelectionState
    {
        /// <summary>
        /// 高速档未选到，此时不管低速档为何值都不做处理:不提示
        /// </summary>
        HighNotFound = -1,
        /// <summary>
        /// 高速档选到了，当前项含低速档，低速档没选到
        /// 此时将返回一个推荐点 (List<double> RecommendPointInLow)
        /// 红色报警:可以选出风机, 提醒低速挡的输入值错误, 背景色变化, 并弹窗, 然后在风机型号窗口显示推荐值
        /// 提示红色：（弹窗、文字）低速挡的工况点与高速挡差异过大,低速档风量的推荐值在XXXm³/h左右,总阻力的推荐值小于YYYPa.
        /// 低速挡颜色：文字红色
        /// </summary>
        LowNotFound = 0,

        /// <summary>
        /// 高速档和低速档都选到了，且高，低速档输入的选型点都在安全范围内
        /// 当前项只有高速档，且高速档输入的选型点在安全范围内，也认为是当前项的高低速档选型点都安全
        /// 正常什么都不提醒
        /// </summary>
        HighAndLowBothSafe = 1,

        /// <summary>
        /// 高速档和低速档都选到了，但高速档输入的选型点不在安全范围内
        /// 当前项只有高速档，高速档选到了，且高速档输入的选型点不在安全范围内
        /// 黄色报警：可以选出风机，提醒高速挡的输入全压过低
        /// 提示：（文字黄色）高速挡输入的总阻力偏小.
        /// </summary>
        HighUnsafe = 2,

        /// <summary>
        /// 高速档和低速档都选到了，但低速档输入的选型点不在安全范围内
        /// 黄色报警：可以选出风机，提醒低速挡的输入全压过低
        /// 提示：（文字黄色）低速挡输入的总阻力偏小
        /// </summary>
        LowUnsafe = 3,

        /// <summary>
        /// 高速档和低速档都选到了，但高，低速档输入的选型点都不在安全范围内
        /// 黄色报警：可以选出风机，提醒高，低速挡的输入全压都过低
        ///提示：（文字黄色） 高、低速档输入的总阻力都偏小.
        /// </summary>
        HighAndLowBothUnsafe = 4,
    }
    public class FanSelectionStateInfo
    {
        /// <summary>
        /// 当前项的选型状态(高速，低速是否选到等)
        /// </summary>
        public EnumFanSelectionState FanSelectionState { get; set; }

        /// <summary>
        /// 当fanSelectionState枚举值为0(LowNotFound), 属性值置为参照点的坐标
        /// </summary>
        public List<double> RecommendPointInLow { get; set; }

        public FanSelectionStateInfo()
        {
            FanSelectionState = EnumFanSelectionState.HighNotFound;
        }
    }
}
