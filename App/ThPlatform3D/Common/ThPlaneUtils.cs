using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO.SVG;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Geometry;
using ThPlatform3D.Model;

namespace ThPlatform3D.Common
{
    public static class ThPlaneUtils
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

        public static double GetFloorBottomElevation(this Dictionary<string, string> properties,List<ThFloorInfo> floorInfos)
        {
            if (properties.ContainsKey(ThSvgPropertyNameManager.FloorNamePropertyName))
            {
                var flrName = properties[ThSvgPropertyNameManager.FloorNamePropertyName];
                var res = floorInfos.Where(o => o.FloorName == flrName);
                if(res.Count()==1)
                {
                    double dValue = 0.0;
                    if (double.TryParse(res.First().Bottom_elevation, out dValue))
                    {
                        return dValue;
                    }
                }                
            }
            return 0.0;
        }

        public static string GetOriginOffset(this Dictionary<string, string> properties)
        {
            if (properties.ContainsKey(ThSvgPropertyNameManager.OriginOffsetPropertyName))
            {
                return properties[ThSvgPropertyNameManager.OriginOffsetPropertyName];
            }
            return "";
        }

        public static Extents2d ToExtents2d(this DBObjectCollection objs)
        {
            var extents = new Extents2d();
            if(objs.Count==0)
            {
                return extents;
            }
            else
            {
                var geoObjs = objs.OfType<Entity>().Where(e => e.Bounds.HasValue).ToCollection();
                if(geoObjs.Count==0)
                {
                    return extents;
                }
                else
                {
                    double minX = 0.0, minY = 0.0, maxX = 0.0, maxY = 0.0;
                    int i = 0;
                    geoObjs.OfType<Entity>().ForEach(e =>
                    {
                        if(i++==0)
                        {
                            minX = e.GeometricExtents.MinPoint.X;
                            minY = e.GeometricExtents.MinPoint.Y;
                            maxX = e.GeometricExtents.MaxPoint.X;
                            maxY = e.GeometricExtents.MaxPoint.Y;
                        }
                        else
                        {
                            if (e.GeometricExtents.MinPoint.X < minX)
                            {
                                minX = e.GeometricExtents.MinPoint.X;
                            }
                            if (e.GeometricExtents.MinPoint.Y < minY)
                            {
                                minY = e.GeometricExtents.MinPoint.Y;
                            }
                            if (e.GeometricExtents.MaxPoint.X > maxX)
                            {
                                maxX = e.GeometricExtents.MaxPoint.X;
                            }
                            if (e.GeometricExtents.MaxPoint.Y > maxY)
                            {
                                maxY = e.GeometricExtents.MaxPoint.Y;
                            }
                        }
                    });
                    extents = new Extents2d(minX, minY, maxX, maxY);
                    return extents;
                }
            }
        }
        public static DBObjectCollection ToDBObjectCollection(this ObjectIdCollection objIds,Database db)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return objIds.ToDBObjectCollection(acadDb);
            }
        }
        public static DBObjectCollection ToDBObjectCollection(this ObjectIdCollection objIds, AcadDatabase acadDb)
        {
            return objIds
                    .OfType<ObjectId>()
                    .Where(o => !o.IsErased)
                    .Select(o => acadDb.Element<Entity>(o)).ToCollection();
        }
        public static void SetLayerOrder(this List<ObjectIdCollection> floorObjIds,List<string> layerPriority)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                // build dict
                var dict = new Dictionary<string, ObjectIdCollection>();
                floorObjIds.ForEach(o =>
                {
                    o.OfType<ObjectId>()
                    .Where(x=>!x.IsErased)
                    .ForEach(e =>
                    {
                        var entity = acadDb.Element<Entity>(e, true);

                        if (dict.ContainsKey(entity.Layer))
                        {
                            dict[entity.Layer].Add(e);
                        }
                        else
                        {
                            var objIds = new ObjectIdCollection() { e };
                            dict.Add(entity.Layer, objIds);
                        }
                    });
                });

                var bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId);
                var btrModelSpace = acadDb.Element<BlockTableRecord>(bt[BlockTableRecord.ModelSpace]);
                var dot = acadDb.Element<DrawOrderTable>(btrModelSpace.DrawOrderTableId, true);

                layerPriority.ForEach(layer =>
                {
                    if (dict.ContainsKey(layer))
                    {
                        dot.MoveToBottom(dict[layer]);
                    }
                });
            }
        }

        public static Dictionary<string, ObjectIdCollection> GroupByLayer(this ObjectIdCollection objIds)
        {            
            using (var acadDb = AcadDatabase.Active())
            {
                var dict = new Dictionary<string, ObjectIdCollection>();
                objIds.OfType<ObjectId>()
                    .Where(x => !x.IsErased)
                    .ForEach(o =>
                {
                    var entity = acadDb.Element<Entity>(o);
                    if (dict.ContainsKey(entity.Layer))
                    {
                        dict[entity.Layer].Add(o);
                    }
                    else
                    {
                        var subObjIds = new ObjectIdCollection() { o };
                        dict.Add(entity.Layer, subObjIds);
                    }
                });
                return dict;
            }
        }

        public static void MoveToBottom(this ObjectIdCollection objIds)
        {
            if (objIds.Count == 0)
            {
                return;
            }
            using (var acadDb = AcadDatabase.Active())
            {
                var bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId);
                var btrModelSpace = acadDb.Element<BlockTableRecord>(bt[BlockTableRecord.ModelSpace]);
                var dot = acadDb.Element<DrawOrderTable>(btrModelSpace.DrawOrderTableId, true);
                dot.MoveToBottom(objIds);
            }
        }

        public static void MoveToTop(this ObjectIdCollection objIds)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId);
                var btrModelSpace = acadDb.Element<BlockTableRecord>(bt[BlockTableRecord.ModelSpace]);
                var dot = acadDb.Element<DrawOrderTable>(btrModelSpace.DrawOrderTableId, true);
                dot.MoveToTop(objIds);
            }
        }

        public static bool IsIncludeHatch(this List<ObjectIdCollection> floorObjIds)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                for (int i = 0; i < floorObjIds.Count; i++)
                {
                    if (floorObjIds[i].OfType<ObjectId>().Where(id => acadDb.Element<Entity>(id) is Hatch).Any())
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public static bool IsInteger(this string content)
        {
            string pattern = @"^\s*\d+\s*$";
            return System.Text.RegularExpressions.Regex.IsMatch(content, pattern);
        }
        public static double GetScaleTextHeight(this double textHeight,double x,double y)
        {
            return textHeight * y / x;
        }
        public static Tuple<double,double> GetDrawScaleValue(this string drawScale)
        {
            // drawScale格式 1:100,1:50 1:1
            if (string.IsNullOrEmpty(drawScale))
            {
                return Tuple.Create(1.0, 1.0);
            }
            else
            {
                var strs = drawScale.Split(':');
                if(strs.Length==0)
                {
                    strs = drawScale.Split('：');
                }
                if(strs.Length == 2)
                {
                    double x = double.Parse(strs[0]);
                    double y = double.Parse(strs[1]);
                    return Tuple.Create(x, y);
                }
                else
                {
                    return Tuple.Create(1.0, 1.0);
                }
            }            
        }
        public static List<int> GetIntegers(this string content)
        {
            var datas = new List<int>();
            string pattern = @"\d+";
            foreach (Match item in Regex.Matches(content, pattern))
            {
                datas.Add(int.Parse(item.Value));
            }
            return datas;
        }
        public static void AddRange(this ObjectIdCollection first, ObjectIdCollection second)
        {
            second.OfType<ObjectId>().ForEach(o => first.Add(o));
        }

        public static Vector3d ToVector3d(this ViewDirection vd)
        {
            switch (vd)
            {
                case ViewDirection.Front:
                    return Vector3d.YAxis;
                case ViewDirection.Back:
                    return Vector3d.YAxis.Negate();
                case ViewDirection.Left:
                    return Vector3d.XAxis;
                case ViewDirection.Right:
                    return Vector3d.XAxis.Negate();
                default:
                    return new Vector3d();
            }            
        }

        public static string GetViewDirectionName(this ViewDirection vd)
        {
            switch (vd)
            {
                case ViewDirection.Front:
                    return "前视图";
                case ViewDirection.Back:
                    return "后视图";
                case ViewDirection.Left:
                    return "左视图";
                case ViewDirection.Right:
                    return "右视图";
                default:
                    return "";
            }
        }
    }
}
