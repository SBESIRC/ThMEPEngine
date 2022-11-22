using System;

namespace ThMEPHVAC.EQPMFanModelEnums
{
    public class DragCalcModel
    {
        /// <summary>
        /// 风管长度：小数点后最多1位
        /// </summary>
        public double DuctLength { get; set; }
        /// <summary>
        /// 比摩阻：小数点后最多1位
        /// </summary>
        public double Friction { get; set; }

        /// <summary>
        /// 局部阻力倍数：小数点后最多1位
        /// </summary>
        public double LocRes { get; set; }
        /// <summary>
        /// 消音器阻力：正整数
        /// </summary>
        public int Damper { get; set; }
        /// <summary>
        /// 风管阻力
        /// </summary>
        public int DuctResistance { get; set; }
        /// <summary>
        /// 末端预留风压
        /// </summary>
        public int EndReservedAirPressure { get; set; }
        /// <summary>
        /// 动压
        /// </summary>
        public int DynPress { get; set; }
        /// <summary>
        /// 计算总阻力
        /// </summary>
        public double CalcResistance { get; set; }
        /// <summary>
        /// 选型系数
        /// </summary>
        public double SelectionFactor { get; set; }
        /// <summary>
        /// 风阻：正整数
        /// </summary>
        public int WindResis { get; set; }

        public void RefeshData() 
        {
            DuctResistance = CalcDuckDrag();
            CalcResistance = CalcDuckAllDrag();
            WindResis = CalcDuckTypeAllDrag();
        }
        /// <summary>
        /// 计算风管阻力
        /// </summary>
        /// <returns></returns>
        public int CalcDuckDrag()
        {
            //计算风管阻力
            return (int)(DuctLength * Friction * (1 + LocRes));
        }
        /// <summary>
        /// 计算总阻力
        /// </summary>
        public double CalcDuckAllDrag()
        {
            //计算总阻力
             return DuctResistance + Damper + EndReservedAirPressure + DynPress;
        }
        public int CalcDuckTypeAllDrag()
        {
            //计算选型总阻力
            var temp = (int)((DuctResistance + Damper + EndReservedAirPressure + DynPress) * SelectionFactor);
            int power = (int)Math.Pow(10, 1);
            var unitsDigit = (temp - temp / power * power) * 10 / power;
            if (unitsDigit != 0)
            {
                temp = temp + 10 - unitsDigit;
            }
            return temp;
        }
    }
}
