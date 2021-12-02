using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ServiceModels
{
    public class ThLigitingWiringModel
    {
        /// <summary>
        /// 连线内容
        /// </summary>
        public string loopType
        {
            get; set;
        }

        /// <summary>
        /// 图层
        /// </summary>
        public string layerType
        {
            get; set;
        }

        /// <summary>
        /// 点位上限
        /// </summary>
        public string pointNum
        {
            get; set;
        }
    }
}
