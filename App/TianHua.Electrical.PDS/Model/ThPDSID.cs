using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSID
    {
        public ThPDSID()
        {
            BlockName = "";
            LoadID = "";
            Description = "";
            CircuitID = new List<string> { "" };
            SourcePanelID = new List<string> { "" };
        }

        /// <summary>
        /// 块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 负载编号
        /// </summary>
        public string LoadID { get; set; }

        /// <summary>
        /// 用户自定义描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 上级配电箱编号
        /// </summary>
        public List<string> SourcePanelID { get; set; }

        /// <summary>
        /// 回路ID
        /// </summary>
        public List<string> CircuitID { get; set; }

        /// <summary>
        /// 回路编号
        /// </summary>
        public List<string> CircuitNumber
        {
            get
            {
                var circuitNumber = new List<string>();
                for (var i = 0; i < CircuitID.Count; i++)
                {
                    if(!string.IsNullOrEmpty( SourcePanelID[i]) && !string.IsNullOrEmpty(CircuitID[i]))
                    {
                        circuitNumber.Add(SourcePanelID[i] + "-" + CircuitID[i]);
                    }
                    else
                    {
                        circuitNumber.Add("");
                    }
                }
                return circuitNumber;
            }
        }
    }
}