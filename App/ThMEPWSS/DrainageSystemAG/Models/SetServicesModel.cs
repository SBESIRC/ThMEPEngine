using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Models
{
    public class SetServicesModel
    {
        SetServicesModel()
        {
            drawingScale = EnumDrawingScale.DrawingScale1_100;
            wasteSewageWaterRiserPipeDiameter = EnumPipeDiameter.DN100;
            wasteSewageVentilationRiserPipeDiameter = EnumPipeDiameter.DN100;
            toiletIsCaisson = false;
            balconyWasteWaterRiserPipeDiameter = EnumPipeDiameter.DN100;
            balconyRiserPipeDiameter = EnumPipeDiameter.DN100;
            condensingRiserPipeDiameter = EnumPipeDiameter.DN50;
            roofRainRiserPipeDiameter = EnumPipeDiameter.DN100;
            maxRoofGravityRainBucketRiserPipeDiameter = EnumPipeDiameter.DN100;
            maxRoofSideDrainRiserPipeDiameter = EnumPipeDiameter.DN100;
            minRoofGravityRainBucketRiserPipeDiameter = EnumPipeDiameter.DN100;
            minRoofSideDrainRiserPipeDiameter = EnumPipeDiameter.DN100;
        }
        public static readonly SetServicesModel Instance = new SetServicesModel();
        /// <summary>
        /// 图纸比例
        /// </summary>
        public EnumDrawingScale drawingScale { get; set; }
        /// <summary>
        /// 废污合流立管直径
        /// </summary>
        public EnumPipeDiameter wasteSewageWaterRiserPipeDiameter { get; set; }
        /// <summary>
        /// 废污合流通气立管直径
        /// </summary>
        public EnumPipeDiameter wasteSewageVentilationRiserPipeDiameter { get; set; }
        /// <summary>
        /// 卫生间是否沉箱
        /// </summary>
        public bool toiletIsCaisson { get; set; }
        /// <summary>
        /// 阳台废水立管直径
        /// </summary>
        public EnumPipeDiameter balconyWasteWaterRiserPipeDiameter { get; set; }
        /// <summary>
        /// 阳台立管直径
        /// </summary>
        public EnumPipeDiameter balconyRiserPipeDiameter { get; set; }
        /// <summary>
        /// 冷凝立管直径
        /// </summary>
        public EnumPipeDiameter condensingRiserPipeDiameter { get; set; }
        /// <summary>
        /// 屋面雨水立管直径
        /// </summary>
        public EnumPipeDiameter roofRainRiserPipeDiameter { get; set; }
        /// <summary>
        /// 大屋面重力雨水斗直径
        /// </summary>
        public EnumPipeDiameter maxRoofGravityRainBucketRiserPipeDiameter { get; set; }
        /// <summary>
        /// 大屋面侧排雨水斗直径
        /// </summary>
        public EnumPipeDiameter maxRoofSideDrainRiserPipeDiameter { get; set; }
        /// <summary>
        /// 小屋面重力雨水斗直径
        /// </summary>
        public EnumPipeDiameter minRoofGravityRainBucketRiserPipeDiameter { get; set; }
        /// <summary>
        /// 小屋面侧排雨水斗直径
        /// </summary>
        public EnumPipeDiameter minRoofSideDrainRiserPipeDiameter { get; set; }
    }
}
