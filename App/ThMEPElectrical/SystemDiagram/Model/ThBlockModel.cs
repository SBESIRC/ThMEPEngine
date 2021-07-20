using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPElectrical.SystemDiagram.Model
{

    public class ThBlockModel
    {
        /// <summary>
        /// 每个块的唯一名称标识，不可重复
        /// </summary>
        public string UniqueName { get; set; }

        /// <summary>
        /// 系统图对应块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 外接属性集合
        /// </summary>
        public Dictionary<string, string> attNameValues { get; set; }

        /// <summary>
        /// 块计数默认数量
        /// </summary>
        public int DefaultQuantity { get; set; } = 0;

        /// <summary>
        /// 是否显示外接属性
        /// </summary>
        public bool ShowAtt { get; set; } = false;

        /// <summary>
        /// 块别名
        /// </summary>
        public string BlockAliasName { get; set; }

        /// <summary>
        /// 块别名/中文名称
        /// </summary>
        public string BlockNameRemark { get; set; }

        /// <summary>
        /// 块索引
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 记录楼层信息
        /// </summary>
        //public int FloorIndex { get; set; }

        /// <summary>
        /// 相对于黄色框的相对位置
        /// </summary>
        public Point3d Position { get; set; }

        /// <summary>
        /// 块计数为0时是否可隐藏
        /// </summary>
        public bool CanHidden { get; set; } = false;

        /// <summary>
        /// 是否显示块的计数
        /// </summary>
        public bool ShowQuantity { get; set; } = false;

        /// <summary>
        /// 块计数地址
        /// </summary>
        public Point3d QuantityPosition { get; set; }

        /// <summary>
        /// 是否显示块的中文名称
        /// </summary>
        public bool ShowText { get; set; } = false;

        /// <summary>
        /// 块中文名称地址
        /// </summary>
        public Point3d TextPosition { get; set; }

        /// <summary>
        /// 是否包含多个块
        /// </summary>
        public bool HasMultipleBlocks { get; set; } = false;

        /// <summary>
        /// 关联的块集合
        /// </summary>
        public List<ThBlockModel> AssociatedBlocks { get; set; }

        /// <summary>
        /// 块计数统计类型
        /// </summary>
        public StatisticType StatisticMode { get; set; } = 0;

        /// <summary>
        /// 统计用外接属性集合
        /// </summary>
        public Dictionary<string, List<string>> StatisticAttNameValues { get; set; }

        /// <summary>
        /// 依赖其他统计模块
        /// </summary>
        public List<string> RelyBlockUniqueNames { get; set; }

        /// <summary>
        /// 依赖其他统计模块的规则
        /// </summary>
        public int DependentStatisticalRule { get; set; } = 1;

        /// <summary>
        /// 膨胀系数
        /// </summary>
        public int CoefficientOfExpansion { get; set; } = 1;

        /// <summary>
        /// 是否有别名
        /// </summary>
        public bool HasAlias { get; set; } = false;

        /// <summary>
        /// 块别名
        /// </summary>
        public List<string> AliasList { get; set; }
    }

    /// <summary>
    /// 统计类型
    /// </summary>
    public enum StatisticType
    {
        /// <summary>
        /// 按块名统计
        /// </summary>
        BlockName,
        /// <summary>
        /// 按外接属性统计
        /// </summary>
        Attributes,
        /// <summary>
        /// 依赖其他模块统计
        /// </summary>
        RelyOthers,
        /// <summary>
        /// 按房间统计
        /// </summary>
        Room,
        /// <summary>
        /// 需要特殊处理
        /// </summary>
        NeedSpecialTreatment,
        /// <summary>
        /// 不需要统计
        /// </summary>
        NoStatisticsRequired
    }
}
