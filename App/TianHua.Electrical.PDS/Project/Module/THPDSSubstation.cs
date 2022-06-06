using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.LowVoltageCabinet;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 变电所
    /// </summary>
    public class THPDSSubstation
    {
        public THPDSSubstation()
        {
            Transformers = new List<THPDSTransformer>();
        }

        /// <summary>
        /// 变电所ID
        /// </summary>
        public string SubstationID { get; set; }

        /// <summary>
        /// 变压器
        /// </summary>
        public List<THPDSTransformer> Transformers { get; set; }
    }

    /// <summary>
    /// 变压器
    /// </summary>
    public class THPDSTransformer
    {
        public THPDSTransformer()
        {
            LowVoltageCabinets = new List<PDSBaseLowVoltageCabinet>();
        }

        /// <summary>
        /// 变压器ID
        /// </summary>
        public string TransformerID { get; set; }
        
        /// <summary>
        /// 低压柜
        /// </summary>
        public List<PDSBaseLowVoltageCabinet> LowVoltageCabinets { get; set; }

        /// <summary>
        /// 计算低压柜选型
        /// </summary>
        public void CalculateLowVoltageCabinetSelection()
        {
            LowVoltageCabinets = new List<PDSBaseLowVoltageCabinet>();//等后续逻辑
        }
    }
}
