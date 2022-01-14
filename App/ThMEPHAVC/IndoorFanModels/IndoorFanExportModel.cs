using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPHVAC.IndoorFanModels
{
    public class IndoorFanExportModel
    {
        public Dictionary<Polyline, List<Polyline>> ExportAreas;
        /// <summary>
        /// 风机类型
        /// </summary>
        public EnumFanType FanType { get; set; }
        public string WorkingName { get; set; }
        public string SavePath { get; set; }
        /// <summary>
        /// 风机信息
        /// </summary>
        public List<IndoorFanBase> TargetFanInfo { get; set; }
        public IndoorFanExportModel()
        {
            TargetFanInfo = new List<IndoorFanBase>();
            ExportAreas = new Dictionary<Polyline, List<Polyline>>();
        }
    }
}
