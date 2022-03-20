using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
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
        public static double CalVariance(List<double> numbers)
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

        /// <summary>
        /// 计算文字居中放置位置
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="finalX"></param>
        /// <param name="finalY"></param>
        /// <param name="strHeight"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Point3d CalCenterPosition(double startX,double startY,double finalX,double finalY,double strHeight,string str)
        {
            double x, y;
            //纵向
            y = (startY - finalY)/2.0 + finalY - strHeight/2.0;
            //横向
            double strWidth = str.Length * strHeight / 2.0;
            x = (startX - finalX) / 2.0 + finalX - strWidth / 2.0;
            Point3d point = new Point3d(x, y, 0);
            return point;
        }


    }
}
