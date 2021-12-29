using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.IndoorFanModels
{
    public class IndoorFanPlaceModel
    {
        public EnumFanType FanType { get; set; }
        /// <summary>
        /// 修正系数
        /// </summary>
        public double CorrectionFactor { get; set; }
        public IndoorFanBase TargetFanInfo { get; set; }
    }
}
