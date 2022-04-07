using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.Uitl;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.PressureDrainageSystem.Utils
{
    public class PressureDrainageUtils
    {
        //通用型函数
        /// <summary>
        /// 定义通用图形数据在CAD图纸中的图层、颜色、线型等属性
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="layer"></param>
        /// <param name="lineType"></param>
        /// <param name="colorIndex"></param>
        /// <param name="lineWeight"></param>
        public static void DefinePropertiesOfCADObjects(Entity entity, string layer, string lineType = "BYLAYER", int colorIndex = (int)ColorIndex.BYLAYER, LineWeight lineWeight = LineWeight.ByLayer)
        {
            entity.Layer = layer;
            entity.ColorIndex = colorIndex;
            entity.Linetype = lineType;
            entity.LineWeight = lineWeight;
            return;
        }
       
        /// <summary>
        /// 定义DBText在CAD图纸中的图层、颜色、线宽等属性
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="layer"></param>
        /// <param name="colorIndex"></param>
        /// <param name="lineType"></param>
        /// <param name="lineWeight"></param>
        public static void DefinePropertiesOfCADDBTexts(DBText dB, string layer, string textString, Point3d alignmentPoint, double height, TextHorizontalMode horizontalMode = TextHorizontalMode.TextLeft, TextVerticalMode verticalMode = TextVerticalMode.TextVerticalMid, int colorIndex = (int)ColorIndex.BYLAYER, string lineType = "BYLAYER", LineWeight lineWeight = LineWeight.ByLayer, double widthFactor = 0.7)
        {
            dB.WidthFactor = widthFactor;
            dB.Layer = layer;
            dB.ColorIndex = colorIndex;
            dB.Linetype = lineType;
            dB.LineWeight = lineWeight;
            dB.HorizontalMode = horizontalMode;
            dB.VerticalMode = verticalMode;
            dB.AlignmentPoint = alignmentPoint;
            dB.TextString = textString;
            //dB.TextStyleId = textStyleId;
            dB.Height = height;
            return;
        }
        public static string AnalysisLine(Line a)
        {
            string s = a.StartPoint.X.ToString() + "," + a.StartPoint.Y.ToString() + "," +
                a.EndPoint.X.ToString() + "," + a.EndPoint.Y.ToString() + ",";
            return s;
        }
        public static string AnalysisLineList(List<Line> a)
        {
            string s = "";
            foreach (var e in a)
            {
                s += AnalysisLine(e);
            }
            return s;
        }
        public static string AnalysisPoly(Polyline a)
        {
            string s = "";
            var e = a.Vertices().Cast<Point3d>().ToList();
            for (int i = 0; i < e.Count; i++)
            {
                s += e[i].X.ToString() + "," + e[i].Y.ToString() + ",";
            }
            return s;
        }

        public static string AnalysisPolyList(List<Polyline> pls)
        {
            string s = "";
            foreach (var e in pls)
            {
                s += AnalysisPoly(e);
                s.Remove(s.Length - 1);
                s += ";";
            }
            return s;
        }
        public static string AnalysisPointList(List<Point3d> points)
        {
            string s = "";
            foreach (var pt in points)
            {
                s += pt.X.ToString() + "," + pt.Y.ToString() + ",";
            }
            return s;
        }

        /// <summary>
        /// 判断两直线是否相交
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool IsIntersected(Line line1, Line line2, double tol, List<VerticalPipeClass> verticalPipes, List<SubmergedPumpClass> submergedPumps)
        {
            List<Point3d> intersectpts = line1.IntersectWithEx(line2).Cast<Point3d>().ToList();
            if (intersectpts.Count > 0)
            {
                double tol1 = 10;
                Point3d pt = intersectpts[0];
                bool cand1 = line1.GetClosestPointTo(pt, false).DistanceTo(line1.StartPoint) > tol1;
                bool cand2 = line1.GetClosestPointTo(pt, false).DistanceTo(line1.EndPoint) > tol1;
                bool cand3 = line2.GetClosestPointTo(pt, false).DistanceTo(line2.StartPoint) > tol1;
                bool cand4 = line2.GetClosestPointTo(pt, false).DistanceTo(line2.EndPoint) > tol1;
                if (cand1 && cand2 && cand3 && cand4)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                Point3d pt1 = line1.StartPoint;
                Point3d pt2 = line1.EndPoint;
                Point3d pt3 = line2.StartPoint;
                Point3d pt4 = line2.EndPoint;
                double dis1 = pt1.DistanceTo(pt3);
                double dis2 = pt1.DistanceTo(pt4);
                double dis3 = pt2.DistanceTo(pt3);
                double dis4 = pt2.DistanceTo(pt4);
                dis1 = dis1 < dis3 ? dis1 : dis3;
                dis2 = dis2 < dis4 ? dis2 : dis4;
                List<Point3d> pts = new();
                verticalPipes.ForEach(o => pts.Add(o.Circle.Center));
                submergedPumps.ForEach(o => pts.Add(o.Extents.CenterPoint()));
                List<Point3d> ptsEnd = new();
                List<Point3d> ptstest = new ();
                ptsEnd.Add(pt1);
                ptsEnd.Add(pt2);
                ptsEnd.Add(pt3);
                ptsEnd.Add(pt4);
                foreach (var pt in ptsEnd)
                {
                    foreach (var ptest in ptsEnd)
                    {
                        if (pt.DistanceTo(ptest) == Math.Min(dis1, dis2))
                        {
                            ptstest.Add(pt);
                            ptstest.Add(ptest);
                            break;
                        }
                    }
                    if (ptstest.Count > 0)
                    {
                        break;
                    }
                }
                int cond_QuitCycle = 0;
                double dis5= 1000;
                foreach (var j in ptstest)
                {
                    foreach (var k in pts)
                    {
                        if (j.DistanceTo(k) < dis5)
                        {
                            cond_QuitCycle = 1;
                            break;
                        }
                    }
                    if (cond_QuitCycle > 0)
                    {
                        break;
                    }
                }
                bool parallel = TestLineParallel(line1, line2, 0);
                if (Math.Min(dis1, dis2) < tol)
                {
                    return true;
                }
                else if (cond_QuitCycle == 0 && parallel && line1.GetClosestPointTo(line2.StartPoint, true).DistanceTo(line2.StartPoint) < 100)
                {
                    return true;
                }
                else
                {
                    double d1 = line1.GetClosestPointTo(line2.StartPoint, false).DistanceTo(line2.StartPoint);
                    double d2 = line1.GetClosestPointTo(line2.EndPoint, false).DistanceTo(line2.EndPoint);
                    double d3 = line2.GetClosestPointTo(line1.StartPoint, false).DistanceTo(line1.StartPoint);
                    double d4 = line2.GetClosestPointTo(line1.EndPoint, false).DistanceTo(line1.EndPoint);
                    dis1 = d1 < d3 ? d1 : d3;
                    dis2 = d2 < d4 ? d2 : d4;
                    if (Math.Min(dis1, dis2) < 50)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        
        /// <summary>
        /// 返回穿过指定对象的几何图形
        /// </summary>
        /// <param name="ptcoll"></param>
        /// <param name="dbObjs"></param>
        /// <returns></returns>
        public static DBObjectCollection GetCrossObjsByPtCollection(Point3dCollection ptcoll, DBObjectCollection dbObjs)
        {
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            var crossObjs = spatialIndex.SelectCrossingPolygon(ptcoll);
            return crossObjs;
        }
       
        /// <summary>
        /// 测试两条直线是否平行
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static bool TestLineParallel(Line a, Line b, double k)
        {
            double dya = Math.Abs(a.StartPoint.Y - a.EndPoint.Y);
            double dxa = Math.Abs(a.StartPoint.X - a.EndPoint.X);
            double dyb = Math.Abs(b.StartPoint.Y - b.EndPoint.Y);
            double dxb = Math.Abs(b.StartPoint.X - b.EndPoint.X);
            if (dxa == dxb && dxa != 0)
            {
                return true;
            }
            else if (Math.Abs(dya / dxa - dyb / dxb) <= k)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
       
        /// <summary>
        /// 判断直线是否水平
        /// </summary>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool TestLineHorizontal(Line line, double toldegree)
        {
            double dy = Math.Abs(line.StartPoint.Y - line.EndPoint.Y);
            double dx = Math.Abs(line.StartPoint.X - line.EndPoint.X);
            if (dx == 0)
            {
                return false;
            }
            else
            {
                double degree = Math.Atan2(dy, dx).AngleToDegree();
                if (degree < toldegree) { return true; }
                else { return false; }
            }
        }
       
        /// <summary>
        /// 判断字符串是否包含汉字
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool TestContainsChineseCharacter(string text)
        {
            return Regex.IsMatch(text, @"[\u4e00-\u9fa5]");
        }
       
        /// <summary>
        /// 判断是否为天正元素
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        public static bool IsTianZhengElement(Entity ent)
        {
            return IsTianZhengElement(ent.GetType());
        }
        
        /// <summary>
        /// 连接认为是一条直线的存在间距的两条直线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="hinderpts"></param>
        /// <returns></returns>
        public static List<Line> ConnectBrokenLine(List<Line> lines, List<Point3d> hinderpts)
        {
            List<Line> connectedLines = new List<Line>();
            List<Line> emilinatedSelfLines = new List<Line>();
            lines.ForEach(o => emilinatedSelfLines.Add(o));
            double tolHinderpts = 300;
            double tolOriHinder = 300;
            double tolBrokenLine = 2000;
            double toldegree = 3;
            List<Polyline> plylist = new List<Polyline>();
            hinderpts.ForEach(o => plylist.Add(o.CreateRectangle(tolHinderpts, tolHinderpts)));
            DBObjectCollection dbObjsOriStart = new DBObjectCollection();
            plylist.ForEach(o => dbObjsOriStart.Add(o));
            for (int i = 0; i < lines.Count; i++)
            {
                emilinatedSelfLines.RemoveAt(i);
                Point3d ptStart = lines[i].StartPoint;
                Point3d ptEnd = lines[i].EndPoint;
                Vector3d SelfLine = new Vector3d(ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y, 0);
                if (GetCrossObjsByPtCollection(ptStart.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0)
                {
                    for (int j = 0; j < emilinatedSelfLines.Count; j++)
                    {
                        Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                        Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                        Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                        if (ptStart.DistanceTo(ptmp1) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptStart.X - ptmp1.X, ptStart.Y - ptmp1.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptStart, ptmp1);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                        else if (ptStart.DistanceTo(ptmp2) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptStart.X - ptmp2.X, ptStart.Y - ptmp2.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptStart, ptmp2);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                    }
                }
                if (GetCrossObjsByPtCollection(ptEnd.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0)
                {
                    for (int j = 0; j < emilinatedSelfLines.Count; j++)
                    {
                        Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                        Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                        Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                        if (ptEnd.DistanceTo(ptmp1) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptEnd.X - ptmp1.X, ptEnd.Y - ptmp1.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptEnd, ptmp1);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                        else if (ptEnd.DistanceTo(ptmp2) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptEnd.X - ptmp2.X, ptEnd.Y - ptmp2.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptEnd, ptmp2);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                    }
                }
                emilinatedSelfLines.Insert(i, lines[i]);
            }
            return connectedLines;
        }
       
        /// <summary>
        /// 判断泛型是否为天正元素
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsTianZhengElement(Type type)
        {
            return type.IsNotPublic && type.Name.StartsWith("Imp") && type.Namespace == "Autodesk.AutoCAD.DatabaseServices";
        }

        /// <summary>
        /// 实现一个Icomparer接口根据指定轴坐标大小排序点集
        /// </summary>
        /// <param name="points"></param>
        /// <param name="dm"></param>
        /// <returns></returns>
        public static List<Point3d> SortPointsBasedSpecailCoordinates(List<Point3d> points, int dm)
        {
            var comparer = new CoordComparer(dm);
            points.Sort(comparer);
            return points;
        }
        public class CoordComparer : IComparer<Point3d>
        {
            public CoordComparer(int dm)
            {
                Dm = dm;
            }
            private int Dm;
            public int Compare(Point3d a, Point3d b)
            {
                var coordsA = new List<double>() { a.X, a.Y, a.Z };
                var coordsB = new List<double>() { b.X, b.Y, b.Z };
                if (coordsA[Dm] == coordsB[Dm]) return 0;
                else if (coordsA[Dm] < coordsB[Dm]) return -1;
                else return 1;
            }
        }

        /// <summary>
        /// 实现一个Icomparer接口排序double
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static List<double> SortDouble(List<double> nums)
        {
            var comparer = new DoubleComparer();
            nums.Sort(comparer);
            return nums;
        }
        public class DoubleComparer : IComparer<double>
        {
            public DoubleComparer()
            {
                    
            }
            public int Compare(double a, double b)
            {
                if (a == b) return 0;
                else if (a < b) return -1;
                else return 1;
            }
        }

        /// <summary>
        /// 实现一个从左到右排序线段的Icomparer类
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Line> SortLinesFromLeftToRight(List<Line> lines)
        {
            var comparer = new LineComparer();
            lines.Sort(comparer);
            return lines;
        }
        public class LineComparer : IComparer<Line>
        {
            public LineComparer()
            {
            }
            public int Compare(Line a, Line b)
            {
                double aX = a.StartPoint.X < a.EndPoint.X ? a.StartPoint.X : a.EndPoint.X;
                double bX = b.StartPoint.X < b.EndPoint.X ? b.StartPoint.X : b.EndPoint.X;
                if (aX == bX) return 0;
                else if (aX < bX) return -1;
                else return 1;
            }
        }

        //业务型函数

        /// <summary>
        /// 实现排序排水井的一个Comparer类
        /// </summary>
        public class DrainWellComparer : IComparer<BlockReference>
        {
            public DrainWellComparer()
            {
            }
            public int Compare(BlockReference br1,BlockReference br2)
            {
                double a = GetSerialOfDrainWell(br1);
                double b = GetSerialOfDrainWell(br2);
                if (a==b) return 0;
                else if (a < b) return -1;
                else return 1;
            }
        }

        /// <summary>
        /// 根据水井编号排序水井
        /// </summary>
        /// <param name="brs"></param>
        /// <returns></returns>
        public static List<BlockReference> SortDrainWellBySerials(List<BlockReference> brs)
        {
            DrainWellComparer comparer = new();
            brs.Sort(comparer);
            return brs;
        }

        /// <summary>
        /// 返回排水井的序号
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public static double GetSerialOfDrainWell(BlockReference br)
        {
            if (br.GetAttributesStrValue("-") == "-") return 0;
            else
            {
                try
                {
                    double serial= double.Parse(br.GetAttributesStrValue("-"));
                    return serial;
                }
                catch { return -1; }
            }                         
        }
        /// <summary>
        /// 计算潜水泵立管合并后的总管径
        /// </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static int CalculateMergePipeDiameter(double Q)
        {
            int diameter = 0;
            if (Q >= 0 && Q <= 22)
            {
                diameter = 65;
            }
            else if (Q > 22 && Q <= 30)
            {
                diameter = 80;
            }
            else if (Q > 30 && Q <= 50)
            {
                diameter = 100;
            }
            else if (Q > 50 && Q <= 100)
            {
                diameter = 150;
            }
            else if (Q > 110)
            {
                diameter = 200;
            }
            return diameter;
        }
       
        /// <summary>
        /// 计算每个立管在使用中的潜水泵数量
        /// </summary>
        /// <param name="allocation"></param>
        /// <returns></returns>
        public static int CalculateUsedPump(string allocation)
        {
            int usedCount = 1;
            if (allocation != "")
            {
                if (allocation[0] == '两')
                {
                    usedCount = 2;
                }
                else if (allocation[0] == '三')
                {
                    usedCount = 3;
                }
                else if (allocation[0] == '四')
                {
                    usedCount = 4;
                }
            }
            return usedCount;
        }
        
        /// <summary>
        /// 计算立管管径
        /// </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static int CalculatePipeDiameter(double Q)
        {
            int diameter = 0;
            if (Q >= 0 && Q <= 12)
            {
                diameter = 50;
            }
            else if (Q > 12 && Q <= 20)
            {
                diameter = 65;
            }
            else if (Q > 20 && Q <= 30)
            {
                diameter = 80;
            }
            else if (Q > 30 && Q <= 50)
            {
                diameter = 100;
            }
            else if (Q > 50 && Q <= 110)
            {
                diameter = 150;
            }
            else
            {
                diameter = 200;
            }
            return diameter;
        }
       
        /// <summary>
        /// 找出穿过指定Extents3d的List<dBText>
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="dBTexts"></param>
        /// <param name="spatialIndex"></param>
        /// <returns></returns>
        public static List<DBText> GetCrossingDbTextsByExtent(Extents3d extent, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            List<DBText> selecteddbTexts = new ();
            List<DBText> texts = new ();
            var ptCollection = extent.ToRectangle().Vertices();
            var crossObjs = spatialIndex.SelectCrossingPolygon(ptCollection);
            selecteddbTexts = crossObjs.Cast<DBText>().ToList();
            foreach (var text in selecteddbTexts)
            {
                DBText dBText = new ();
                dBText.Position = text.Position;
                dBText.Height = text.Height;
                dBText.TextString = text.TextString;
                texts.Add(dBText);
            }
            return texts;
        }
       
        /// <summary>
        /// 收集天正立管
        /// </summary>
        /// <param name="labelLines"></param>
        /// <param name="texts"></param>
        /// <param name="entities"></param>
        public static void CollectTianzhengVerticalPipes(List<Line> labelLines, List<DBText> texts, List<Entity> entities)
        {
            foreach (var entity in entities.Where(e => IsTianZhengElement(e)).ToList())
            {
                var entitieslist = entity.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                if (entitieslist.OfType<Line>().Any())
                {
                    foreach (var line in entitieslist)
                    {
                        if (IsTianZhengElement(line))
                        {
                            var tmplist = line.ExplodeToDBObjectCollection().OfType<DBText>().ToList();
                            if (tmplist.Count == 1)
                            {
                                LabelClass labelClass = new ();
                                var e = tmplist[0];
                                var t = e.TextString;
                                if (!ThRainSystemService.IsWantedLabelText(t)) return;
                                texts.Add(e);
                                labelLines.AddRange(entitieslist.OfType<Line>().Where(e => e.Length > 0));
                                return;
                            }
                        }
                    }
                }
            }
        }
       
        /// <summary>
        /// Entity转换为Line
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="lin"></param>
        /// <returns></returns>
        public static bool TryConvertToLineSegment(Entity ent, out Line lin)
        {
            if (ent is Line line)
            {
                lin = line;
                return true;
            }
            {
                var p1 = ent.GetType().GetProperty("StartPoint");
                if (p1 == null)
                {
                    lin = default;
                    return false;
                }
                var p2 = ent.GetType().GetProperty("EndPoint");
                if (p2 == null)
                {
                    lin = default;
                    return false;
                }
                if (p1.PropertyType != typeof(Point3d))
                {
                    lin = default;
                    return false;
                }
                if (p2.PropertyType != typeof(Point3d))
                {
                    lin = default;
                    return false;
                }
                var pt1 = (Point3d)p1.GetValue(ent);
                var pt2 = (Point3d)p2.GetValue(ent);
                lin = new Line(pt1, pt2);
                return true;
            }
        }

        /// <summary>
        /// 判断两组DBText列表的文字内容是否相同
        /// </summary>
        /// <param name="texts1"></param>
        /// <param name="texts2"></param>
        /// <returns></returns>
        public static bool IsSameDBTextsList(List<DBText> texts1, List<DBText> texts2)
        {
            foreach (var a in texts1)
            {
                int cond_found = 0;
                foreach (var b in texts2)
                {
                    if (a.TextString == b.TextString)
                    {
                        cond_found = 1;
                        break;
                    }
                }
                if (cond_found == 0) return false;
            }
            foreach (var a in texts2)
            {
                int cond_found = 0;
                foreach (var b in texts1)
                {
                    if (a.TextString == b.TextString)
                    {
                        cond_found = 1;
                        break;
                    }
                }
                if (cond_found == 0) return false;
            }
            return true;
        }

        /// <summary>
        /// 创建楼层边界点集
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static List<List<Point3dCollection>> CreateFloorAreaList(List<ThIfcSpatialElement> elements)//创建所有楼层的分区列表
        {
            using var acadDatabase = AcadDatabase.Active();
            var FloorAreaList = new List<List<Point3dCollection>>();
            foreach (var obj in elements)//遍历楼层
            {
                if (obj is ThStoreys)
                {
                    var sobj = obj as ThStoreys;
                    var br = acadDatabase.Element<BlockReference>(sobj.ObjectId);
                    if (!br.IsDynamicBlock) continue;
                    if (sobj.StoreyNumber.Trim().StartsWith("B"))
                    {
                        var rectList = ThWCompute.CreateRectList(sobj);
                        FloorAreaList.Add(rectList);//分区的多段线添加
                    }
                }
            }

            return FloorAreaList;
        }

        /// <summary>
        /// 实现一个Icomparer接口根据潜水泵位置排序集水井
        /// </summary>
        /// <param name="points"></param>
        /// <param name="dm"></param>
        /// <returns></returns>
        public static List<WellInfo> SortWellsBasedSpecailPumps(List<WellInfo> wells, Point3d pt)
        {
            var comparer = new WellInfoComparer(pt);
            wells.Sort(comparer);
            return wells;
        }
        public class WellInfoComparer : IComparer<WellInfo>
        {
            public WellInfoComparer(Point3d pt)
            {
                Pt = pt;
            }
            private Point3d Pt;
            public int Compare(WellInfo a, WellInfo b)
            {
                if (a.Location.DistanceTo(Pt) == b.Location.DistanceTo(Pt)) return 0;
                else if (a.Location.DistanceTo(Pt) < b.Location.DistanceTo(Pt)) return -1;
                else return 1;
            }
        }
    }
}