using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    /// <summary>
    /// 封闭避难层（间）、避难走道模型
    /// </summary>
    public class RefugeRoomAndCorridorModel : ThFanVolumeModel
    {

        /// <summary>
        /// 风量
        /// </summary>
        public double WindVolume
        {
            get
            {
                return Math.Round(Area_Net * AirVol_Spec);
            }
        }
        /// <summary>
        /// 净面积
        /// </summary>
        public double Area_Net { get; set; }

        /// <summary>
        /// 单位风量
        /// </summary>
        public double AirVol_Spec { get; set; }

        /// <summary>
        /// 应用场景
        /// </summary>
        public override string FireScenario
        {
            get
            {
                return "封闭避难层（间）、避难走道";
            }
        }


    }
}
