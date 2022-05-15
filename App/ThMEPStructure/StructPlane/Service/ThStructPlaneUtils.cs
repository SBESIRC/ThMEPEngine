using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.StructPlane.Service
{
    internal static class ThStructPlaneUtils
    {
        public static string GetFillColor(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.FillColorPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetLineType(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.LineTypePropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetCategory(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.CategoryPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static double GetFloorBottomElevation(this Dictionary<string, string> properties)
        {
            if(properties.ContainsKey(ThSvgPropertyNameManager.FloorBottomElevationPropertyName))
            {
                var value = properties[ThSvgPropertyNameManager.FloorBottomElevationPropertyName];
                double dValue = 0.0;
                if(double.TryParse(value,out dValue))
                {
                    return dValue;
                }                
            }
            return 0.0;
        }
        public static double GetFloorHeight(this Dictionary<string, string> properties)
        {
            if (properties.ContainsKey(ThSvgPropertyNameManager.FloorElevationPropertyName))
            {
                var value = properties[ThSvgPropertyNameManager.FloorElevationPropertyName];
                double dValue = 0.0;
                if (double.TryParse(value, out dValue))
                {
                    return dValue;
                }
            }
            return 0.0;
        }
        public static string GetSpec(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.SpecPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetElevation(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.ElevationPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        private static object GetPropertyValue(this Dictionary<string, object> properties, string key)
        {
            foreach (var item in properties)
            {
                if (item.Key.ToUpper() == key.ToUpper())
                {
                    return item.Value;
                }
            }
            return null;
        }
        /// <summary>
        /// 阿拉伯数字转换成中文数字
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string NumToChinese(this string x)
        {
            string[] pArrayNum = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            //为数字位数建立一个位数组
            string[] pArrayDigit = { "", "十", "百", "千" };
            //为数字单位建立一个单位数组
            string[] pArrayUnits = { "", "万", "亿", "万亿" };
            var pStrReturnValue = ""; //返回值
            var finger = 0; //字符位置指针
            var pIntM = x.Length % 4; //取模
            int pIntK;
            if (pIntM > 0)
                pIntK = x.Length / 4 + 1;
            else
                pIntK = x.Length / 4;
            //外层循环,四位一组,每组最后加上单位: ",万亿,",",亿,",",万,"
            for (var i = pIntK; i > 0; i--)
            {
                var pIntL = 4;
                if (i == pIntK && pIntM != 0)
                    pIntL = pIntM;
                //得到一组四位数
                var four = x.Substring(finger, pIntL);
                var P_int_l = four.Length;
                //内层循环在该组中的每一位数上循环
                for (int j = 0; j < P_int_l; j++)
                {
                    //处理组中的每一位数加上所在的位
                    int n = Convert.ToInt32(four.Substring(j, 1));
                    if (n == 0)
                    {
                        if (j < P_int_l - 1 && Convert.ToInt32(four.Substring(j + 1, 1)) > 0 && !pStrReturnValue.EndsWith(pArrayNum[n]))
                            pStrReturnValue += pArrayNum[n];
                    }
                    else
                    {
                        if (!(n == 1 && (pStrReturnValue.EndsWith(pArrayNum[0]) | pStrReturnValue.Length == 0) && j == P_int_l - 2))
                            pStrReturnValue += pArrayNum[n];
                        pStrReturnValue += pArrayDigit[P_int_l - j - 1];
                    }
                }
                finger += pIntL;
                //每组最后加上一个单位:",万,",",亿," 等
                if (i < pIntK) //如果不是最高位的一组
                {
                    if (Convert.ToInt32(four) != 0)
                        //如果所有4位不全是0则加上单位",万,",",亿,"等
                        pStrReturnValue += pArrayUnits[i - 1];
                }
                else
                {
                    //处理最高位的一组,最后必须加上单位
                    pStrReturnValue += pArrayUnits[i - 1];
                }
            }
            return pStrReturnValue;
        }
        public static List<double> GetDoubles(this string content)
        {
            var datas = new List<double>();
            string pattern = @"\d+([.]\d+)?";
            foreach (Match item in Regex.Matches(content, pattern))
            {
                datas.Add(double.Parse(item.Value));
            }
            return datas;
        }
        public static Polyline CreateRectangle(this Point3d center, 
            Vector3d xVec, Vector3d yVec,
            double xLength, double yLength)
        {
            var pt1 = center + xVec.GetNormal().MultiplyBy(xLength / 2.0) + yVec.GetNormal().MultiplyBy(yLength / 2.0);
            var pt2 = center - xVec.GetNormal().MultiplyBy(xLength / 2.0) + yVec.GetNormal().MultiplyBy(yLength / 2.0);
            var pt3 = center - xVec.GetNormal().MultiplyBy(xLength / 2.0) - yVec.GetNormal().MultiplyBy(yLength / 2.0);
            var pt4 = center + xVec.GetNormal().MultiplyBy(xLength / 2.0) - yVec.GetNormal().MultiplyBy(yLength / 2.0);
            var pts = new Point3dCollection { pt1, pt2, pt3, pt4 };
            return pts.CreatePolyline();
        }
        public static double GetPolylineMaxSegmentLength(this Polyline polyline)
        {
            double result = 0.0;
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                var segType = polyline.GetSegmentType(i);
                if (segType == SegmentType.Line)
                {
                    var lineSeg = polyline.GetLineSegmentAt(i);
                    if (lineSeg.Length > result)
                    {
                        result = lineSeg.Length;
                    }
                }
                else if (segType == SegmentType.Arc)
                {
                    var arc = polyline.GetArcSegmentAt(i).ToArc();
                    if (arc.Length > result)
                    {
                        result = arc.Length;
                    }
                    arc.Dispose();
                }
            }
            return result;
        }
    }
}
