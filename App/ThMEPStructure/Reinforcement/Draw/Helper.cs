using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.Reinforcement.Draw
{
    class Helper
    {
        /// <summary>
        /// 将纵筋规格解析出每个不同大小的规格的钢筋各有多少个，一共有多少个
        /// </summary>
        /// <param name="str"></param>
        public static int AnalyseZongJinStr(string str)
        {
            int result = 0;
            return result;
        }

        public static int SumLinkNum(string str)
        {
            int result = 0;
            return result;
        }

        public static double CalScale(string str)
        {
            double result = 100.0 / (double)int.Parse(str.Substring(2));
            return result;
        }

        /// <summary>
        /// 计算方差，平方的均值减去均值的平方
        /// </summary>
        /// <param name="numbers"></param>
        public static double calVariance(List<double> numbers)
        {
            double squareSum = 0;
            double sum = 0;
            foreach(var num in numbers)
            {
                squareSum += num * num;
                sum += num;
            }

            return (squareSum / numbers.Count) - (sum / numbers.Count) * (sum / numbers.Count);
        }

    }
}
