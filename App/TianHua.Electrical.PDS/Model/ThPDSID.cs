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
            PowerTransformerCircuitList = new List<Tuple<string, string>>();
            Storeys = new List<int>();
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

        /// <summary>
        /// 变压器回路编号系列
        /// </summary>
        public List<Tuple<string, string>> PowerTransformerCircuitList { get; set; }

        /// <summary>
        /// 楼层序列
        /// </summary>
        public List<int> Storeys { get; set; }

        public ThPDSID Clone()
        {
            var id = new ThPDSID
            {
                BlockName = this.BlockName,
                LoadID = this.LoadID,
                Description = this.Description,
                DefaultDescription = this.DefaultDescription,
                CircuitIDList = new List<string>(),
                SourcePanelIDList = new List<string>(),
                Storeys = new List<int>(),
            };
            this.CircuitIDList.ForEach(circuitID => id.CircuitIDList.Add(circuitID));
            this.SourcePanelIDList.ForEach(sourcePanelID => id.SourcePanelIDList.Add(sourcePanelID));
            this.Storeys.ForEach(storey => id.Storeys.Add(storey));
            return id;
        }
    }
}