using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 信息匹配查看器 模块
    /// </summary>
    [Serializable]
    public class InformationMatchViewerModule : PDSProjectModule
    {
        /// <summary>
        /// 回路信息
        /// </summary>
        public List<PDSDWGLoopInfo> LoopInfo { get; set; }
        /// <summary>
        /// 负载信息
        /// </summary>
        public List<PDSDWGLoadInfo> LoadInfo { get; set; }
        public Action DataChanged;
    }

    [Serializable]
    public class PDSDWGLoopInfo
    {
        private string id;
        private ThPDSCircuitType type;
        private ThPDSLoad superiorDistributionBox;
        private string dwgName;

        public string ID
        {
            set { id = value; }
            get
            {
                if (string.IsNullOrEmpty(id))
                    return "无";
                else
                    return id;
            }
        }
        public ThPDSCircuitType Type
        {
            get { return type; }
            set { type = value; }
        }
        public ThPDSLoad SuperiorDistributionBox
        {
            get { return superiorDistributionBox; }
            set { superiorDistributionBox = value; }
        }
        public string DWGName
        {
            set { dwgName = value; }
            get { return dwgName; }
        }
    }
    [Serializable]
    public class PDSDWGLoadInfo
    {
        private string id;
        private ThPDSLoadTypeCat_1 type;
        private double power;
        private string dwgName;

        public string ID
        {
            set { id = value; }
            get
            {
                if (string.IsNullOrEmpty(id))
                    return "无";
                else
                    return id;
            }
        }
        public ThPDSLoadTypeCat_1 Type
        {
            get { return type; }
            set { type = value; }
        }
        public double Power
        {
            get { return power; }
            set { power = value; }
        }
        public string DWGName
        {
            set { dwgName = value; }
            get { return dwgName; }
        }
    }
}
