using System;
using System.Collections.Generic;
using System.Linq;

namespace TianHua.Electrical.PDS.Model
{
    [Serializable]
    public class ThPDSID
    {
        public ThPDSID()
        {
            BlockName = "";
            LoadID = "";
            Description = "";
            DefaultDescription = "";
            CircuitIDList = new List<string> { "" };
            SourcePanelIDList = new List<string> { "" };
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
        private string _description;
        public string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(_description))
                {
                    return _description;
                }
                else
                {
                    return DefaultDescription;
                }
            }
            set
            {
                _description = value;
            }
        }

        /// <summary>
        /// 默认负载描述
        /// </summary>
        public string DefaultDescription { get; set; }

        /// <summary>
        /// 上级配电箱编号序列
        /// </summary>
        public List<string> SourcePanelIDList { get; set; }

        /// <summary>
        /// 上级配电箱编号
        /// </summary>
        private string _sourcePanelID = "";
        public string SourcePanelID
        {
            get
            {
                if (string.IsNullOrEmpty(_sourcePanelID))
                {
                    return SourcePanelIDList.Last();
                }
                return _sourcePanelID;
            }
            set
            {
                _sourcePanelID = value;
            }
        }

        /// <summary>
        /// 回路ID序列
        /// </summary>
        public List<string> CircuitIDList { get; set; }

        /// <summary>
        /// 回路ID
        /// </summary>
        private string _circuitID = "";
        public string CircuitID
        {
            get
            {
                if (string.IsNullOrEmpty(_circuitID))
                {
                    return CircuitIDList.Last();
                }
                return _circuitID;
            }
            set
            {
                _circuitID = value;
            }
        }

        /// <summary>
        /// 回路编号
        /// </summary>
        public string CircuitNumber
        {
            get
            {
                if (!string.IsNullOrEmpty(SourcePanelID))
                {
                    return SourcePanelID + "-" + CircuitID;
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 回路编号序列
        /// </summary>
        public List<string> CircuitNumberList
        {
            get
            {
                var circuitNumber = new List<string>();
                for (var i = 0; i < CircuitIDList.Count; i++)
                {
                    if (!string.IsNullOrEmpty(SourcePanelIDList[i]) && !string.IsNullOrEmpty(CircuitIDList[i]))
                    {
                        circuitNumber.Add(SourcePanelIDList[i] + "-" + CircuitIDList[i]);
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