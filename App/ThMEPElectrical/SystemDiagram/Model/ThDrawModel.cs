using System;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 绘制模型
    /// </summary>
    public class ThDrawModel
    {
        public string FireDistrictName { get; set; }

        public DataSummary Data { get; set; }

        public bool DrawCircuitName { get; set; } = false;

        public string WireCircuitName { get; set; }

        public int FloorCount { get; set; } = 1;
    }
}
