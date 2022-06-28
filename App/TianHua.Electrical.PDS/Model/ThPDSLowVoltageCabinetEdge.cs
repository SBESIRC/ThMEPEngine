namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSLowVoltageCabinetEdge
    {
        /// <summary>
        /// 上级低压柜编号
        /// </summary>
        public string SourceLowVoltageCabinetID { get; set; }

        /// <summary>
        /// 回路ID
        /// </summary>
        public string CircuitID { get; set; }

        /// <summary>
        /// 回路编号
        /// </summary>
        public string CircuitNumber
        {
            get
            {
                return SourceLowVoltageCabinetID.Replace("L", "W") + "-" + CircuitID;
            }
        }

        /// <summary>
        /// 下级节点
        /// </summary>
        public ThPDSCircuitGraphNode Target { get; set; }

        public ThPDSLowVoltageCabinetEdge()
        {
            SourceLowVoltageCabinetID = "";
            CircuitID = "";
        }

        public ThPDSLowVoltageCabinetEdge(string sourceLowVoltageCabinetID, string circuitID, ThPDSCircuitGraphNode target)
        {
            SourceLowVoltageCabinetID = sourceLowVoltageCabinetID;
            CircuitID = circuitID;
            Target = target;
        }
    }
}
