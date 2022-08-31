using System.ComponentModel;

namespace ThMEPTCH.Model
{
    public class ThTCHTwtPipeDimStyle
    {
        /// <summary>
        /// 显示标注
        /// </summary>
        public bool ShowDim { get; set; }

        /// <summary>
        /// 管径样式
        /// </summary>
        public DnStyle DnStyle { get; set; }

        /// <summary>
        /// 坡度样式
        /// </summary>
        public GradientStyle GradientStyle { get; set; }

        /// <summary>
        /// 管长样式
        /// </summary>
        public LengthStyle LengthStyle { get; set; }

        /// <summary>
        /// 排列方式样式
        /// </summary>
        public bool ArrangeStyle { get; set; }

        /// <summary>
        /// 分隔符样式
        /// </summary>
        public DelimiterStyle DelimiterStyle { get; set; }

        /// <summary>
        /// 标注排序样式
        /// </summary>
        public SortStyle SortStyle { get; set; }
    }

    public enum DnStyle
    {
        /// <summary>
        /// 不标注
        /// </summary>
        [Description("不标注")]
        NoDimension = 0,

        /// <summary>
        /// DN500
        /// </summary>
        [Description("DN500")]
        Type1 = 1,

        /// <summary>
        /// 500
        /// </summary>
        [Description("500")]
        Type2 = 2,
    }

    public enum GradientStyle
    {
        /// <summary>
        /// 不标注
        /// </summary>
        [Description("不标注")]
        NoDimension = 0,

        /// <summary>
        /// i=0.003
        /// </summary>
        [Description("i=0.003")]
        Type1 = 1,

        /// <summary>
        /// i=3‰
        /// </summary>
        [Description("i=3‰")]
        Type2 = 2,

        /// <summary>
        /// 0.003
        /// </summary>
        [Description("0.003")]
        Type3 = 3,

        /// <summary>
        /// 3‰
        /// </summary>
        [Description("3‰")]
        Type4 = 4,

        /// <summary>
        /// i0.003
        /// </summary>
        [Description("i0.003")]
        Type5 = 5,

        /// <summary>
        /// i3‰
        /// </summary>
        [Description("i3‰")]
        Type6 = 6,
    }

    public enum LengthStyle
    {
        /// <summary>
        /// 不标注
        /// </summary>
        [Description("不标注")]
        NoDimension = 0,

        /// <summary>
        /// L=12.3m
        /// </summary>
        [Description("L=12.3m")]
        Type1 = 1,

        /// <summary>
        /// L=12.3
        /// </summary>
        [Description("L=12.3")]
        Type2 = 2,

        /// <summary>
        /// L12.3m
        /// </summary>
        [Description("L12.3m")]
        Type3 = 3,

        /// <summary>
        /// L12.3
        /// </summary>
        [Description("L12.3")]
        Type4 = 4,

        /// <summary>
        /// 12.3m
        /// </summary>
        [Description("12.3m")]
        Type5 = 5,

        /// <summary>
        /// 12.3
        /// </summary>
        [Description("12.3")]
        Type6 = 6,
    }

    public enum DelimiterStyle
    {
        /// <summary>
        /// 空格
        /// </summary>
        [Description("空格")]
        Blank = 0,

        /// <summary>
        /// -
        /// </summary>
        [Description("-")]
        ShortTerm = 1,

        /// <summary>
        /// /
        /// </summary>
        [Description("/")]
        Diagonal = 2,
    }

    public enum SortStyle
    {
        /// <summary>
        /// 管径 坡度 管长
        /// </summary>
        [Description("管径 坡度 管长")]
        Type0 = 0,

        /// <summary>
        /// 管径 管长 坡度
        /// </summary>
        [Description("管径 管长 坡度")]
        Type1 = 1,

        /// <summary>
        /// 坡度 管径 管长
        /// </summary>
        [Description("坡度 管径 管长")]
        Type2 = 2,

        /// <summary>
        /// 坡度 管长 管径
        /// </summary>
        [Description("坡度 管长 管径")]
        Type3 = 3,

        /// <summary>
        /// 管长 管径 坡度
        /// </summary>
        [Description("管长 管径 坡度")]
        Type4 = 4,

        /// <summary>
        /// 管长 坡度 管径
        /// </summary>
        [Description("管长 坡度 管径")]
        Type5 = 5,
    }
}
