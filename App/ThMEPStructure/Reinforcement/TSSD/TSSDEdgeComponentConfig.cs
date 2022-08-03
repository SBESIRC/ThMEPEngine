namespace ThMEPStructure.Reinforcement.TSSD
{
    public class TSSDEdgeComponentConfig
    {
        /// <summary>
        /// 计算书软件
        /// </summary>
        public string CalculationSoftware { get; set; } = "";
        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefix { get; set; } = "仅构造约束";
        /// <summary>
        /// 墙柱图层
        /// </summary>
        public string WallColumnLayer { get; set; } = "边构";
        /// <summary>
        /// 排序方式
        /// </summary>
        public string SortWay { get; set; } = "从左到右，从下到上";
        /// <summary>
        /// 引线形式
        /// </summary>
        public string LeaderType { get; set; } = "折线引出";
        /// <summary>
        /// 归并尺寸
        /// </summary>
        public string MergeSize { get; set; } = "";
        /// <summary>
        /// 标注位置
        /// </summary>
        public string MarkPosition { get; set; } = "右上";
        /// <summary>
        /// 归并配箍率
        /// </summary>
        public string MergeStirrupRatio { get; set; }
        /// <summary>
        /// 归并配筋率
        /// </summary>
        public string MergeReinforceRatio { get; set; }
        /// <summary>
        /// 归并考虑墙体
        /// </summary>

        public bool MergeConsiderWall { get; set; }

        /// <summary>
        /// 构造前缀
        /// </summary>
        public string ConstructPrefix { get; set; } = "GBZ";
        /// <summary>
        /// 构造前缀的起始编号
        /// </summary>
        public string ConstructPrefixStartNumber { get; set; } = "1";
        /// <summary>
        /// 约束前缀
        /// </summary>
        public string ConstraintPrefix { get; set; } = "YBZ";
        /// <summary>
        /// 约束前缀的起始编号
        /// </summary>
        public string ConstraintPrefixStartNumber { get; set; } = "1";
    }
}
