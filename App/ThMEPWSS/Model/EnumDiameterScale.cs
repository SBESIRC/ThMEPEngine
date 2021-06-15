using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Model
{
    /// <summary>
    /// 管径枚举
    /// （管径这里没有小数，可以直接使用Int表示，如果有小数通过描述表述与解析）
    /// </summary>
    public enum EnumPipeDiameter
    {
        DN50 = 50,
        DN75 = 75,
        DN100 = 100,
        DN125 = 125,
        DN150 = 150,
        DN200 = 200,
    }


    /// <summary>
    /// 图纸比例枚举值
    /// </summary>
    public enum EnumDrawingScale
    {
        /// <summary>
        /// 图纸比例1:50
        /// </summary>
        [Description("1:50")]
        DrawingScale1_50 = 50,
        /// <summary>
        /// 图纸比例1:100
        /// </summary>
        [Description("1:100")]
        DrawingScale1_100 = 100,
        /// <summary>
        /// 图纸比例1:150
        /// </summary>
        [Description("1:150")]
        DrawingScale1_150 = 150
    }
}
