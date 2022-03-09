using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanLayout.Service
{
    public class ThFanLayoutDealService
    {
        public static string GetFanVolume(double volume)
        {
            string strVolume = "风量：" + volume.ToString() + "CMH";
            return strVolume;
        }
        public static double GetFanVolum(string volum)
        {
            var str = Regex.Replace(volum, @"[^\d.\d]", "");
            double resDouble = double.Parse(str);
            return resDouble;
        }
        public static string GetFanPower(double power)
        {
            string strPower = "电量：" + power.ToString()+"W";
            return strPower;
        }
        public static double GetFanPower(string power)
        {
            var str = Regex.Replace(power, @"[^\d.\d]", "");
            double resDouble = double.Parse(str);
            return resDouble;
        }
        public static void GetDuctWidthAndHeight(string size,out double width,out double height)
        {
            var str = size.Split('x');
            width = double.Parse(str[0]);
            height = double.Parse(str[1]);
        }
        public static string GetFanWeight(double weight)
        {
            string strWeight = weight.ToString() + "kg";
            return strWeight;
        }
        public static string GetFanNoise(double noise)
        {
            string strNoise = noise.ToString() + "dB(A)";
            return strNoise;
        }
        public static string GetAirPortMarkSize(double width, double heigth)
        {
            string strWidth = width.ToString();
            string strHeigth = heigth.ToString();
            string airPortMarkSize = strWidth + "x" + strHeigth;
            return airPortMarkSize;
        }
        public static string GetAirPortMarkVolume(double volume, double parameter = 0.8)
        {
            int tmpInt = (int)(volume * parameter) / 10;
            if ((int)(volume * parameter) % 10 != 0)
            {
                tmpInt = tmpInt + 1;
            }
            volume = tmpInt * 10;
            var airPortMarkVolume = volume.ToString();
            return airPortMarkVolume;
        }

        public static string GetFanHoleSize(double width, double heigth, double space = 100)
        {
            string strWidth = (width + space).ToString();
            string strHeigth = (heigth + space).ToString();
            string fanHoleSize = "留洞：" + strWidth + "x" + strHeigth+"(H)";
            return fanHoleSize;
        }
        public static string GetFanHoleMark(int type, double heigth = 800)
        {
            string fanHoleMark = "顶边贴梁底";
            if (0 == type)
            {
                fanHoleMark = "顶边贴梁底";
            }
            else
            {
                string strHeigth = heigth.ToString("0.00");
                fanHoleMark = "洞底标高："+"h+" + strHeigth+"m";
            }
            return fanHoleMark;
        }
        public static string GetAirPortHeightMark(int type, double heigth = 800)
        {
            string fanHoleMark = "顶边贴梁底";
            if (0 == type)
            {
                fanHoleMark = "顶边贴梁底";
            }
            else
            {
                string strHeigth = heigth.ToString("0.00");
                fanHoleMark = "风口底边距地" + strHeigth + "m";
            }
            return fanHoleMark;
        }
        public static double GetFontHeight(int type, string strScale)
        {
            double height = 0.0;
            switch(type)
            {
                case 0:
                    {
                        if(strScale == "1:50")
                        {
                            height = 150.0;
                        }
                        else if (strScale == "1:100")
                        {
                            height = 300.0;
                        }
                        else if (strScale == "1:150")
                        {
                            height = 450.0;
                        }
                        else if (strScale == "1:200")
                        {
                            height = 600.0;
                        }
                    }
                    break;
                case 1:
                    {
                        if (strScale == "1:50")
                        {
                            height = 50.0;
                        }
                        else if (strScale == "1:100")
                        {
                            height = 100.0;
                        }
                        else if (strScale == "1:150")
                        {
                            height = 150.0;
                        }
                        else if (strScale == "1:200")
                        {
                            height = 200.0;
                        }
                    }
                    break;
                default:
                    break;
            }
            return height;
        }

        public static double GetAirPortSpeed(double volume,double length,double width)
        {
            //风速 = 风机风量 * 0.8 * 3600 /（(风口长 / 1000) * (风速宽 / 1000)）
            double speed = volume * 0.8 / (length / 1000 * (width / 1000)*3600);
            return speed;
        }
        public static double GetAirPipeSpeed(double volume ,double width,double heigth)
        {
            double speed = volume / ((width / 1000) * (heigth / 1000)*3600);
            return speed;
        }
    }
}
