namespace ThMEPTCH.Model
{
    public class ThTCHTelecClapboard
    {
        /// <summary>
        /// 是否有隔板
        /// </summary>
        public bool HaveClapboard { get; set; }

        /// <summary>
        /// 隔板数据1
        /// </summary>
        public string ClapboardData { get; set; }

        /// <summary>
        /// 隔板数据2
        /// </summary>
        public string ClapboardData2 { get; set; }

        /// <summary>
        /// 隔板向量1(端点-起点）
        /// </summary>
        public string ClapboardMainBr { get; set; }

        /// <summary>
        /// 隔板向量2
        /// </summary>
        public string ClapboardIdSubBr { get; set; }
    }
}
