using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.LoadCalculation.Extension
{
    public static class MathExtension
    {
        /// <summary>
        /// 按指定位数向上取余
        /// </summary>
        /// <param name="number"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public static double Ceiling(this double number,int decimals)
        {
            double multiple = Math.Pow( 10,  decimals);
            return Math.Ceiling(number * multiple) / multiple;
        }

        public static int CeilingInteger(this int number, int decimals)
        {
            int remainder = number % decimals;
            if (remainder != 0)
                return number + decimals - remainder;
            else
                return number;
        }
    }
}
