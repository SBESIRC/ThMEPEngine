namespace ThMEPHVAC.Service
{
    public class ThAirSpeedCalculator
    {
        /// <summary>
        /// 计算方形风口的风速
        /// </summary>
        /// <param name="singleAirportAirVolume">单位：m3/h</param>
        /// <param name="length">单位：mm</param>
        /// <param name="width">单位：mm</param>
        /// <returns></returns>
        public static double RecAirPortSpeed(double singleAirportAirVolume,double length,double width)
        {
            var ll = length.MmToMeter();
            var ww = width.MmToMeter();
            var area = ThMEPHAVCCommon.GetArea(ll, ww);
            return singleAirportAirVolume / (area * 3600.0);
        }
        /// <summary>
        /// 计算圆形风口的风速
        /// </summary>
        /// <param name="singleAirportAirVolume">单位：m3/h</param>
        /// <param name="diameter">单位：mm</param>
        /// <returns></returns>
        public static double CircleAirPortSpeed(double singleAirportAirVolume, double diameter)
        {
            var dia = diameter.MmToMeter();
            var area = ThMEPHAVCCommon.GetArea(dia/2.0);
            return singleAirportAirVolume / (area * 3600.0);
        }
    }
}
