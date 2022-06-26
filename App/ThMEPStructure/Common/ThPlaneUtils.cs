using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.Common
{
    internal static class ThPlaneUtils
    {
        public static string GetFloorRange(this double flrBottomEle,List<ThFloorInfo> FloorInfos)
        {
            var result = "";
            var stdFloors = FloorInfos.Where(o =>
            {
                double bottomElevation = 0.0;
                if (double.TryParse(o.Bottom_elevation, out bottomElevation))
                {
                    if (Math.Abs(bottomElevation - flrBottomEle) <= 1e-4)
                    {
                        return true;
                    }
                }
                return false;
            });
            if (stdFloors.Count() == 1)
            {
                var stdFlr = stdFloors.First().StdFlrNo;
                var floors = FloorInfos.Where(o => o.StdFlrNo == stdFlr);
                if (floors.Count() == 1)
                {
                    result = floors.First().FloorNo.NumToChinese();
                }
                else if (floors.Count() > 1)
                {
                    var startRange = floors.First().FloorNo.NumToChinese();
                    var endRange = floors.Last().FloorNo.NumToChinese();
                    result = startRange + "~" + endRange;
                }
            }
            return result;
        }
        /// <summary>
        /// 阿拉伯数字转换成中文数字
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string NumToChinese(this string x)
        {
            string[] pArrayNum = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            if(x.Trim().Length ==1)
            {
                switch (x.Trim())
                {
                    case "0":
                        return pArrayNum[0];
                        case "1":
                        return pArrayNum[1];
                    case "2":
                        return pArrayNum[2];
                    case "3":
                        return pArrayNum[3];
                    case "4":
                        return pArrayNum[4];
                    case "5":
                        return pArrayNum[5];
                    case "6":
                        return pArrayNum[6];
                    case "7":
                        return pArrayNum[7];
                    case "8":
                        return pArrayNum[8];
                    case "9":
                        return pArrayNum[9];
                    default:
                        return "";
                }
            }
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

        public static double GetFloorBottomElevation(this Dictionary<string, string> properties)
        {
            if (properties.ContainsKey(ThSvgPropertyNameManager.FloorBottomElevationPropertyName))
            {
                var value = properties[ThSvgPropertyNameManager.FloorBottomElevationPropertyName];
                double dValue = 0.0;
                if (double.TryParse(value, out dValue))
                {
                    return dValue;
                }
            }
            return 0.0;
        }
        public static Extents2d ToExtents2d(this DBObjectCollection objs)
        {
            var extents = new Extents2d();
            double minX = double.MaxValue, minY = double.MaxValue,
                maxX = double.MinValue, maxY = double.MinValue;
            objs.OfType<Curve>().ForEach(entity =>
            {
                if (!entity.IsErased && entity.GeometricExtents != null)
                {
                    if (entity.GeometricExtents.MinPoint.X < minX)
                    {
                        minX = entity.GeometricExtents.MinPoint.X;
                    }
                    if (entity.GeometricExtents.MinPoint.Y < minY)
                    {
                        minY = entity.GeometricExtents.MinPoint.Y;
                    }
                    if (entity.GeometricExtents.MaxPoint.X > maxX)
                    {
                        maxX = entity.GeometricExtents.MaxPoint.X;
                    }
                    if (entity.GeometricExtents.MaxPoint.Y > maxY)
                    {
                        maxY = entity.GeometricExtents.MaxPoint.Y;
                    }
                }
            });
            extents = new Extents2d(minX, minY, maxX, maxY);
            return extents;
        }
        public static DBObjectCollection ToDBObjectCollection(this ObjectIdCollection objIds,Database db)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return objIds
                    .OfType<ObjectId>()
                    .Where(o => !o.IsErased)
                    .Select(o => acadDb.Element<Entity>(o)).ToCollection();
            }
        }
    }
}
