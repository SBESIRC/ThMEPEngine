using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Model
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

        public THPDSSubstation(string substationID)
        {
            SubstationID = substationID;
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
            LowVoltageCabinets = new List<PDSLowVoltageCabinet>();
        }

        /// <summary>
        /// 变压器ID
        /// </summary>
        public string TransformerID { get; set; }

        /// <summary>
        /// 低压柜
        /// </summary>
        public List<PDSLowVoltageCabinet> LowVoltageCabinets { get; set; }
    }

    public class PDSLowVoltageCabinet
    {
        public PDSLowVoltageCabinet() 
        {
            Edges = new List<ThPDSLowVoltageCabinetEdge>();
        }

        /// <summary>
        /// 低压柜ID
        /// </summary>
        public string LowVoltageCabinetID { get; set; }

        public List<ThPDSLowVoltageCabinetEdge> Edges { get; set; }
    }
}
