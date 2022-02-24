namespace TianHua.Structure.WPF.UI.HuaRunPeiJin
{
    internal class EdgeComponentInfo
    {
        /// <summary>
        /// 编号
        /// eg. GBZ24,GBZ1
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 规格
        /// eg. 一字型: 1650x200,L型：200x800,200,300
        /// </summary>
        public string Spec { get; set; }
        /// <summary>
        /// 形状
        /// eg. 一形，L形，T形
        /// </summary>
        public string Shape { get; set; }
        /// <summary>
        /// 类型
        /// eg 标准，标准C,非标
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 类型代号，用于标识标准-A,标准-B,标准Cal-A,标准Cal-B
        /// 取值为: A 或 B
        /// </summary>
        public string TypeCode { get; set; }
        /// <summary>
        /// 配筋率
        /// </summary>
        public double ReinforceRatio { get; set; }
        /// <summary>
        /// 配箍率
        /// </summary>
        public double StirrupRatio { get; set; }
    }
}
