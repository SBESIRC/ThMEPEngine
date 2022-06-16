namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSFilterBlockInfo
    {
        /// <summary>
        /// 块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 属性
        /// </summary>
        public string Properties { get; set; }

        /// <summary>
        /// 过滤方式
        /// </summary>
        public FilteringMethod FilteringMethod { get; set; }

        public ThPDSFilterBlockInfo()
        {
            BlockName = "";
            Properties = "";
            FilteringMethod = FilteringMethod.Ignore;
        }
    }

    public enum FilteringMethod
    {
        Ignore,
        Attached,
        Terminal,
    }
}
