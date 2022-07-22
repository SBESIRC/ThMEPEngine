using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;
using Linq2Acad;
using ThCADExtension;

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

        public static void UpdateLineType(this Dictionary<string, object> properties,string lineType)
        {
            string lineTypeKWord = ThSvgPropertyNameManager.LineTypePropertyName;
            if(properties.ContainsKey(lineTypeKWord))
            {
                properties[lineTypeKWord] = lineType;
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

        public static string GetDescription(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.DescriptionPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }

        public static string GetDirection(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.DirPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
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
        public static string GetName(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.NamePropertyName);
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
        public static Vector3d ToVector(this string vecContent)
        {
            if (string.IsNullOrEmpty(vecContent))
            {
                return new Vector3d();
            }
            else
            {
                var values = vecContent.Split(',');
                if (values.Length == 2)
                {
                    double value1 = 0.0, value2 = 0.0;
                    if (double.TryParse(values[0], out value1) &&
                       double.TryParse(values[1], out value2))
                    {
                        return new Vector3d(value1, value2, 0);
                    }
                    else
                    {
                        return new Vector3d();
                    }
                }
                else
                {
                    return new Vector3d();
                }
            }
        }
        public static DBObjectCollection Clip(this Entity polygon,
           DBObjectCollection curves, bool inverted = false)
        {
            var results = new DBObjectCollection();
            if (polygon is Polyline polyline)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(polyline, curves, inverted);
            }
            else if (polygon is MPolygon mPolygon)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(mPolygon, curves, inverted);
            }
            return results.OfType<Curve>().ToCollection();
        }

        public static DBObjectCollection CollinearMerge(this DBObjectCollection lines)
        {
            var grouper = new ThColliearLineGrouper(lines);
            var groups = grouper.Group();
            // 再对组内的线按连接关系分组
            return groups.Select(g => Create(g)).ToCollection();
        }

        private static Line Create(DBObjectCollection colliearLines)
        {
            var ptPair = ThGeometryTool.GetCollinearMaxPts(colliearLines.OfType<Line>().ToList());
            return new Line(ptPair.Item1, ptPair.Item2);
        }

        public static List<string> FilterSlabElevations(this List<string> elevations,double flrHeight)
        {
            return elevations.Where(o =>
            {
                if (string.IsNullOrEmpty(o))
                {
                    return false;
                }
                else
                {
                    double tempV = 0.0;
                    if (double.TryParse(o, out tempV))
                    {
                        return Math.Abs(tempV - flrHeight) <= 1.0 ? false : true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }).ToList();
        }        
        public static string GetFloorRange(this List<ThFloorInfo> floorInfos,double flrBottomEle)
        {
            var result = "";
            var stdFloors = floorInfos.GetFloors(flrBottomEle);
            if (stdFloors.Count() == 1)
            {
                var stdFlr = stdFloors.First().StdFlrNo;
                var floors = floorInfos.Where(o => o.StdFlrNo == stdFlr);
                if (floors.Count() == 1)
                {
                    result = floors.First().FloorNo.NumToChinese() + "层结构平面图";
                }
                else if (floors.Count() > 1)
                {
                    var startRange = floors.First().FloorNo.NumToChinese();
                    var endRange = floors.Last().FloorNo.NumToChinese();
                    result = startRange + "~" + endRange + "层结构平面图";
                }
            }
            return result;
        }
        public static string GetFloorHeightRange(this List<ThFloorInfo> floorInfos, double flrBottomEle)
        {
            // 获取楼层标高范围
            var result = "";
            var stdFloors = floorInfos.GetFloors(flrBottomEle);
            if (stdFloors.Count() == 1)
            {
                var stdFlr = stdFloors.First().StdFlrNo;
                var floors = floorInfos.Where(o => o.StdFlrNo == stdFlr);
                if (floors.Count() > 0)
                {
                    var lastFlr = floors.Last();
                    double lastFlrHeight = 0.0;
                    double lastFlrBottomElevation = 0.0;                    
                    if (double.TryParse(lastFlr.Height, out lastFlrHeight) &&
                        double.TryParse(lastFlr.Bottom_elevation, out lastFlrBottomElevation))
                    {
                        double topElevation = (lastFlrBottomElevation + lastFlrHeight) / 1000.0;
                        double bottomElevation = flrBottomEle / 1000.0;
                        result = bottomElevation.ToString("N3") +"m"+ " ~ " +
                            topElevation.ToString("N3")+"m" + " 墙柱平面图";
                    }
                }
            }
            return result;
        }
        public static List<ThFloorInfo> GetFloors(this List<ThFloorInfo> floorInfos, double flrBottomEle)
        {
            // 根据楼层底部标高获取楼层
            return floorInfos.Where(o => Math.Abs(o.BottomElevation - flrBottomEle) <= 1e-4).ToList();
        }
        public static void ImportStruPlaneTemplate(this Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            using (var blockDb = AcadDatabase.Open(ThCADCommon.StructPlanePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                ThPrintLayerManager.AllLayers.ForEach(layer =>
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), true);
                });

                // 导入样式
                ThPrintStyleManager.AllTextStyles.ForEach(style =>
                {
                    acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(style), false);
                });

                // 导入块
                ThPrintBlockManager.AllBlockNames.ForEach(b =>
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(b), true);
                });
            }
        }
    }
}
