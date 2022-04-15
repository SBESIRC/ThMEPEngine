namespace ThMEPHVAC.EQPMFanModelEnums
{
    public class FanPrefixDictDataModel
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int No { get; set; }

        /// <summary>
        /// 风机用途
        /// </summary>
        public EnumScenario FanUse { get; set; }

        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Explain { get; set; }

    }
}
