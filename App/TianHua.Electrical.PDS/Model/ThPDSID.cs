using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSID
    {
        /// <summary>
        /// 块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 负载编号
        /// </summary>
        public string LoadID { get; set; } = "";

        /// <summary>
        /// 用户自定义描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 回路ID
        /// </summary>
        public string CircuitID { get; set; }

        /// <summary>
        /// 回路编号
        /// </summary>
        public string CircuitNumber { get; set; } = "";

        /// <summary>
        /// 上级配电箱编号
        /// </summary>
        public string SourcePanelID { get; set; }
    }
}