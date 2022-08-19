using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage
{
    public static class ThGarageUtils
    {
        public static bool IsLessThan45Degree(Point3d firstStart, Point3d firstEnd, Point3d secondStart, Point3d secondEnd)
        {
            var outerAng = CalculateTwoLineOuterAngle(firstStart, firstEnd, secondStart, secondEnd);
            if (outerAng - Math.PI / 4 <= 1e-5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsLessThan45Degree(this Line first, Line second)
        {
            return IsLessThan45Degree(first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint);
        }
        public static double CalculateTwoLineOuterAngle(this Line first, Line second)
        {
            return CalculateTwoLineOuterAngle(first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint);
        }
        public static double CalculateTwoLineOuterAngle(Point3d firstStart, Point3d firstEnd, Point3d secondStart, Point3d secondEnd)
        {
            var distance = new List<double>
            {
                firstStart.DistanceTo(secondStart),
                firstStart.DistanceTo(secondEnd),
                firstEnd.DistanceTo(secondStart),
                firstEnd.DistanceTo(secondEnd)
            };

            var mindistance = double.MaxValue;
            var index = 0;
            for (int i = 0; i < distance.Count; i++)
            {
                if (distance[i] < mindistance)
                {
                    mindistance = distance[i];
                    index = i;
                }
            }

            var a = index / 2;
            var b = index % 2;
            var firstList = new List<Point3d>
            {
                firstStart,
                firstEnd
            };
            var secondList = new List<Point3d>
            {
                secondStart,
                secondEnd
            };

            var first = firstList[1 - a].GetVectorTo(firstList[a]);
            var second = secondList[b].GetVectorTo(secondList[1 - b]);

            return first.GetAngleTo(second);
        }
        public static void RepairLineDir(this List<Line> lines, Point3d startPt)
        {
            var newStartPt = new Point3d(startPt.X, startPt.Y, startPt.Z);
            for (int i = 0; i < lines.Count; i++)
            {
                if (newStartPt.DistanceTo(lines[i].StartPoint) >
                    newStartPt.DistanceTo(lines[i].EndPoint))
                {
                    var tempPt = lines[i].StartPoint;
                    lines[i].StartPoint = lines[i].EndPoint;
                    lines[i].EndPoint = tempPt;
                }
            }
        }
        public static List<Line> Explode(this List<Curve> curves)
        {
            var results = new List<Line>();
            curves.ForEach(c =>
            {
                if (c is Line line)
                {
                    results.Add(line.Clone() as Line);
                }
                else if (c is Polyline poly)
                {
                    results.AddRange(poly.ToLines());
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }
        public static List<Line> Trim(this List<Line> lines, Entity polygon)
        {
            // Clip的结果中可能有点（DBPoint)，这里可以忽略点
            var results = new DBObjectCollection();
            if (polygon is Polyline polyline)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(polyline, lines.ToCollection());
            }
            else if (polygon is MPolygon mPolygon)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(mPolygon, lines.ToCollection());
            }
            var curves = results.OfType<Curve>().ToCollection();
            return ThLaneLineEngine.Explode(curves).Cast<Line>().ToList();
        }

        public static int GetLoopCharLength(this int loopNumber)
        {
            if (loopNumber >= 1 && loopNumber < 100)
            {
                return 2;
            }
            else
            {
                return loopNumber.ToString().Length;
            }
        }
        public static string GetLightNumber(this int number, int loopCharLength)
        {
            var code = number.ToString().PadLeft(loopCharLength, '0');
            return ThGarageLightCommon.LightNumberPrefix + code;
        }
        public static List<Line> Preprocess(this List<Line> curves)
        {
            if (curves.Count == 0)
            {
                return new List<Line>();
            }
            else
            {
                var lines = curves.ToCollection();
                var cleanInstance = new ThLaneLineCleanService();
                lines = cleanInstance.CleanNoding(lines);
                return lines.OfType<Line>().ToList();
            }
        }
        public static List<Line> CleanNoding(this List<Line> curves)
        {
            if (curves.Count == 0)
            {
                return new List<Line>();
            }
            else
            {
                var lines = curves.ToCollection();
                var cleanInstance = new ThLaneLineCleanService();
                lines = cleanInstance.CleanNoding(lines);
                return lines.OfType<Line>().ToList();
            }
        }

        public static bool IsCollinear(this Line first, Line second, double tolerance)
        {
            return ThGeometryTool.IsCollinearEx(
                first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint, tolerance);
        }
        /// <summary>
        /// 减去一根线上重叠的线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="overlapLines">与line是共线的</param>
        /// <returns></returns>
        public static List<Line> Difference(this Line line, List<Line> overlapLines)
        {
            var pts = new List<Point3d>();
            overlapLines.ForEach(e =>
            {
                pts.Add(e.StartPoint);
                pts.Add(e.EndPoint);
            });
            if (pts.Count > 0)
            {
                var splitLines = line.Split(pts);
                // 分割的线与传入的重叠线有重叠，
                return splitLines.OfType<Line>().Where(l =>
                {
                    var midPt = l.StartPoint.GetMidPt(l.EndPoint);
                    foreach (var p in overlapLines)
                    {
                        if (midPt.IsPointOnLine(p))
                        {
                            return false;
                        }
                    }
                    return true;
                }).ToList();
            }
            else
            {
                return new List<Line> { line.Clone() as Line };
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pts">分割点，点要在线上</param>
        /// <returns></returns>
        public static List<Line> Split(this Line line, List<Point3d> pts)
        {
            var results = new List<Line>();
            var newPts = pts.Where(p => ThGeometryTool.IsPointInLine(line.StartPoint, line.EndPoint, p, 0.0))
                .OrderBy(p => p.DistanceTo(line.StartPoint)).ToList();
            var startPt = line.StartPoint;
            for (int i = 0; i < newPts.Count; i++)
            {
                if (startPt.DistanceTo(newPts[i]) > 1e-6)
                {
                    results.Add(new Line(startPt, newPts[i]));
                }
                startPt = newPts[i];
            }
            if (startPt.DistanceTo(line.EndPoint) > 1e-6)
            {
                results.Add(new Line(startPt, line.EndPoint));
            }
            return results;
        }

        public static List<Point3d> Distinct(this List<Point3d> pts, Tolerance tolerance)
        {
            var results = new List<Point3d>();
            while (pts.Count > 0)
            {
                results.Add(pts[0]);
                pts.RemoveAt(0);
                var equalPts = pts.Where(p => p.IsEqualTo(results.Last(), tolerance)).ToList();
                pts = pts.Where(p => !equalPts.Contains(p)).ToList();
            }
            return results;
        }

        /// <summary>
        /// 判断两个向量是逆时针
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsAntiClockwise(this Vector3d a, Vector3d b)
        {
            var x1 = a.X;
            var y1 = a.Y;
            var x2 = b.X;
            var y2 = b.Y;
            var value = x1 * y2 - x2 * y1;
            return value > 0.0;
        }
        /// <summary>
        /// 判断两个向量是顺时针
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsClockwise(this Vector3d a, Vector3d b)
        {
            var x1 = a.X;
            var y1 = a.Y;
            var x2 = b.X;
            var y2 = b.Y;
            var value = x1 * y2 - x2 * y1;
            return value < 0.0;
        }
        /// <summary>
        /// 获取跳接些的偏移方向
        /// </summary>
        /// <param name="lineVec"></param>
        /// <returns></returns>
        public static Vector3d GetAlignedDimensionTextDir(this Vector3d lineVec, double tolerance = 1e-6)
        {
            // 参照对齐标注的文字方向
            if (lineVec.IsCodirectionalTo(Vector3d.YAxis, new Tolerance(tolerance, tolerance)) ||
                lineVec.IsCodirectionalTo(Vector3d.YAxis.Negate(), new Tolerance(tolerance, tolerance)))
            {
                // 沿+Y，-Y方向，往-X
                if (lineVec.Y > 0)
                {
                    return lineVec.GetPerpendicularVector();
                }
                else
                {
                    return lineVec.GetPerpendicularVector().Negate();
                }
            }
            else
            {
                if (lineVec.X > 0)
                {
                    return lineVec.GetPerpendicularVector();
                }
                else
                {
                    return lineVec.GetPerpendicularVector().Negate();
                }
            }
        }

        /// <summary>
        /// 把前后连接的线转成Polyline
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public static Polyline ToPolyline(this List<Line> edges, double tolerance = 1.0)
        {
            if (edges.Count == 0)
            {
                return new Polyline();
            }
            else if (edges.Count == 1)
            {
                var pts = new Point3dCollection { edges[0].StartPoint, edges[0].EndPoint };
                return pts.CreatePolyline(false);
            }
            else
            {
                var linkPt = edges[0].FindLinkPt(edges[1], tolerance);
                if (!linkPt.HasValue)
                {
                    var pts = new Point3dCollection { edges[0].StartPoint, edges[0].EndPoint };
                    return pts.CreatePolyline(false);
                }
                else
                {
                    var startPt = linkPt.Value.GetNextLinkPt(edges[0].StartPoint, edges[0].EndPoint);
                    var pts = new Point3dCollection() { startPt };
                    for (int i = 0; i < edges.Count; i++)
                    {
                        var nextPt = startPt.GetNextLinkPt(edges[i].StartPoint, edges[i].EndPoint);
                        pts.Add(nextPt);
                        startPt = nextPt;
                    }
                    return pts.CreatePolyline(false);
                }
            }
        }

        public static Polyline Reverse(this Polyline polyline)
        {
            var pts = new Point3dCollection();
            for (int i = polyline.NumberOfVertices - 1; i >= 0; i--)
            {
                pts.Add(polyline.GetPoint3dAt(i));
            }
            return pts.CreatePolyline(polyline.Closed);
        }

        /// <summary>
        /// 计算点距离Poly起点的长度
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="poly">必须是Line组成多段线</param>
        /// <returns></returns>
        public static double? DistanceTo(this Point3d pt, Polyline poly)
        {
            if (!pt.IsPointOnCurve(poly))
            {
                return null;
            }
            double distance = 0.0;
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var lineSeg = poly.GetLineSegmentAt(i);
                if (lineSeg.IsOn(pt, new Tolerance(1.0, 1.0)))
                {
                    var newPt = pt.GetProjectPtOnLine(lineSeg.StartPoint, lineSeg.EndPoint);
                    distance += lineSeg.StartPoint.DistanceTo(newPt);
                    break;
                }
                else
                {
                    distance += lineSeg.Length;
                }
            }
            return distance;
        }
        public static Point3d? FindLinkPt(this Line first, Line second, double tolerance = 1.0)
        {
            if (first.EndPoint.DistanceTo(second.StartPoint) <= tolerance)
            {
                return first.EndPoint;
            }
            if (first.EndPoint.DistanceTo(second.EndPoint) <= tolerance)
            {
                return first.EndPoint;
            }
            if (first.StartPoint.DistanceTo(second.StartPoint) <= tolerance)
            {
                return first.StartPoint;
            }
            if (first.StartPoint.DistanceTo(second.EndPoint) <= tolerance)
            {
                return first.StartPoint;
            }
            return null;
        }

        public static bool IsGeometryEqual(this Line first, Line second, Tolerance tolerance)
        {
            if (first.StartPoint.IsEqualTo(second.StartPoint, tolerance) &&
                first.EndPoint.IsEqualTo(second.EndPoint, tolerance))
            {
                return true;
            }
            if (first.StartPoint.IsEqualTo(second.EndPoint, tolerance) &&
                first.EndPoint.IsEqualTo(second.StartPoint, tolerance))
            {
                return true;
            }
            return false;
        }
        public static bool GeometryContains(this List<Line> lines, Line line, Tolerance tolerance)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (line.IsGeometryEqual(lines[i], tolerance))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 按当前Ucs矩阵,对线进行排序
        /// 先X方向，再Y方向，剩余按与X方向的夹角排序
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="wcsToUcs"></param>
        /// <returns></returns>
        public static List<Line> Sort(this List<Line> lines, Matrix3d wcsToUcs)
        {
            // 根据当前Ucs，对Line集合，优先按水平和垂直排序
            // 其它再按角度从小到大排序
            var dict = new Dictionary<Line, Line>();
            lines.ForEach(l =>
            {
                var clone = l.Clone() as Line;
                clone.TransformBy(wcsToUcs);
                if (!dict.ContainsKey(clone))
                {
                    dict.Add(clone, l);
                }
            });
            var totalLines = dict.Keys.ToList();
            // 先获取X方向线
            var xDirLines = dict.Keys.Where(l => IsXDirAngle(l.Angle.RadToAng())).OrderBy(
                l => l.LineDirection().GetAngleTo(wcsToUcs.CoordinateSystem3d.Xaxis)).ToList();
            totalLines = totalLines.Where(l => !xDirLines.Contains(l)).ToList();

            // 获取Y方向线
            var yDirLines = totalLines.Where(l => IsYDirAngle(l.Angle.RadToAng())).OrderBy(
                l => l.LineDirection().GetAngleTo(wcsToUcs.CoordinateSystem3d.Yaxis)).ToList();
            totalLines = totalLines.Where(l => !yDirLines.Contains(l)).ToList();

            // 剩余按其它角度排序
            totalLines = totalLines.OrderBy(l => l.LineDirection().GetAngleTo(wcsToUcs.CoordinateSystem3d.Xaxis)).ToList();

            var results = new List<Line>();
            results.AddRange(xDirLines);
            results.AddRange(yDirLines);
            results.AddRange(totalLines);
            return results.Select(l => dict[l]).ToList();
        }
        private static bool IsXDirAngle(double ang, double tolerance = 1.0)
        {
            ang %= 180.0;
            return ang <= 1.0 || Math.Abs(ang - 180.0) <= tolerance;
        }
        private static bool IsYDirAngle(double ang, double tolerance = 1.0)
        {
            ang %= 180.0;
            return Math.Abs(ang - 90.0) <= tolerance;
        }
        public static List<Line> Normalize(this List<Line> lines)
        {
            //单位化、修正方向
            return lines.Select(o => ThGarageLightUtils.NormalizeLaneLine(o)).ToList();
        }
        public static Tuple<Line, Point3d> FindPriorityStart(this List<Line> lines, double tolerance = 1.0)
        {
            var instance = ThQueryLineService.Create(lines);
            for (int i = 0; i < lines.Count; i++)
            {
                var current = lines[i];
                var querys = instance.Query(current.StartPoint, tolerance);
                querys.Remove(current);
                if (querys.Count == 0)
                {
                    return Tuple.Create(current, current.StartPoint);
                }
                else
                {
                    querys = instance.Query(current.EndPoint, tolerance);
                    querys.Remove(current);
                    if (querys.Count == 0)
                    {
                        return Tuple.Create(current, current.EndPoint);
                    }
                }
            }
            return null;
        }
        public static Point3dCollection RemoveNeibourDuplicatedPoints(this Point3dCollection pts, double tolerance = 1.0)
        {
            // pts是一段Polyline的点集合， 移除相邻的重复点
            var result = new Point3dCollection();
            if (pts.Count == 0)
            {
                return result;
            }
            result.Add(pts[0]);
            for (int i = 1; i < pts.Count; i++)
            {
                if (pts[i].DistanceTo(result[result.Count - 1]) <= tolerance)
                {
                    continue;
                }
                else
                {
                    result.Add(pts[i]);
                }
            }
            return result;
        }
        public static Polyline BufferPath(this Polyline path, double offsetDis, bool isSingle = true)
        {
            var poly = new Polyline();
            if (path.Length < 1.0)
            {
                return poly;
            }
            var objs = new DBObjectCollection() { path };
            var res = isSingle ? objs.SingleSidedBuffer(offsetDis) : objs.Buffer(offsetDis);
            if (res.Count > 0)
            {
                poly = res.OfType<Polyline>().OrderByDescending(o => o.Area).First();
            }
            return poly;
        }
        public static DBObjectCollection Query(this Point3d pt,
            ThCADCoreNTSSpatialIndex spatialIndex, double envelopLength = 1.0)
        {
            var envelop = ThDrawTool.CreateSquare(pt, envelopLength);
            return spatialIndex.SelectCrossingPolygon(envelop);
        }
        public static DBObjectCollection Delete(this ThRegionBorder border, ThLightArrangeParameter arrangeParameter, Database db)
        {
            using (AcadDatabase acadDb = AcadDatabase.Use(db))
            {
                var results = new DBObjectCollection(); // 返回删除的元素
                var lightBlks = border.Lights.ToCollection();
                var numberTexts = border.Texts.ToCollection();
                var tchCableTrays = border.TCHCableTrays.ToCollection();
                var lightWires = new DBObjectCollection();
                border.JumpWires.ForEach(j => lightWires.Add(j));
                border.CenterLines.ForEach(c => lightWires.Add(c));
                border.SideLines.ForEach(s => lightWires.Add(s));

                var lightingLines = new List<Line>();
                lightingLines.AddRange(border.FirstLightingLines);
                lightingLines.AddRange(border.SecondLightingLines);
                lightingLines.AddRange(border.FdxCenterLines);
                var queryService = new ThQueryLightWireService(
                    lightBlks, numberTexts, lightWires, lightingLines, tchCableTrays, arrangeParameter);
                queryService.Query();

                // 将查询的结果删除
                results = results.Union(queryService.QualifiedBlks);
                results = results.Union(queryService.QualifiedTexts);
                results = results.Union(queryService.QualifiedTCHCableTrays);
                results = results.Union(queryService.QualifiedCurves);
                results = results.Distinct();

                results.OfType<Entity>().ToList().ForEach(e =>
                {
                    e.UpgradeOpen();
                    e.Erase();
                    e.DowngradeOpen();
                });
                return results;
            }
        }
        public static bool IsSameDirection(this Vector3d first, Vector3d second)
        {
            return first.DotProduct(second) > 0.0;
        }
        public static bool IsOppositeDirection(this Vector3d first, Vector3d second)
        {
            return first.DotProduct(second) < 0.0;
        }
        public static void ThDispose(this DBObjectCollection dbObjs)
        {
            dbObjs.OfType<DBObject>().ToList().ForEach(o => o.Dispose());
        }
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            var dict = new Dictionary<TKey, TValue>();
            var dictAsIDictionary = (IDictionary<TKey, TValue>)dict;
            foreach (var property in keyValuePairs)
            {
                (dictAsIDictionary).Add(property);
            }
            return dict;
        }
        public static Point3d GetPolylinePt(this Polyline polyline, double distance)
        {
            return polyline.GetPointAtDist(distance);
        }
        public static bool IsOn(this Point3d pt, Curve curve, double tolerance = 0.0001)
        {
            return pt.DistanceTo(curve.GetClosestPointTo(pt, false)) <= tolerance;
        }
        /// <summary>
        /// 判断点在线的左边
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static bool IsLeftOfLine(this Point3d pt, Point3d sp, Point3d ep)
        {
            double tmpx = (sp.X - ep.X) / (sp.Y - ep.Y) * (pt.Y - ep.Y) + ep.X;
            if (tmpx > pt.X)//当tmpx>p.x的时候，说明点在线的左边，小于在右边，等于则在线上。
                return true;
            return false;
        }
        /// <summary>
        /// 判断点在线的右边
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static bool IsRightOfLine(this Point3d pt, Point3d sp, Point3d ep)
        {
            double tmpx = (sp.X - ep.X) / (sp.Y - ep.Y) * (pt.Y - ep.Y) + ep.X;
            if (tmpx < pt.X)//当tmpx<p.x的时候，说明点在线的右边，小于在左边，等于则在线上。
                return true;
            return false;
        }

        public static List<Line> CleanNoding(List<Line> lightLines, List<Line> nonLightLines, List<Line> singleRowLines)
        {
            var resultsCollection = new DBObjectCollection();

            // 线优化处理
            LineSimplify(lightLines).OfType<Line>().ToList().ForEach(o => resultsCollection.Add(o));
            LineSimplify(nonLightLines).OfType<Line>().ToList().ForEach(o => resultsCollection.Add(o));
            LineSimplify(singleRowLines).OfType<Line>().ToList().ForEach(o => resultsCollection.Add(o));

            resultsCollection = ThLaneLineExtendEngine.LineExtendAndMerge(resultsCollection);
            resultsCollection = ThLaneLineEngine.Noding(resultsCollection);
            resultsCollection = ThLaneLineEngine.CleanZeroCurves(resultsCollection);

            return resultsCollection.OfType<Line>().ToList();
        }

        private static DBObjectCollection LineSimplify(List<Line> lines)
        {
            var results = ThLaneLineEngine.Explode(lines.ToCollection());
            results = ThLaneLineMergeExtension.Merge(results);
            return results;
        }

        public static Entity BufferEx(this Entity polygon, double distance)
        {
            var objs = new DBObjectCollection();
            if (polygon is Polyline polyline)
            {
                objs = polyline.Buffer(distance * -1.0);
            }
            else if (polygon is MPolygon mPolyggon)
            {
                objs = mPolyggon.Buffer(distance * -1.0, true);
            }

            // 按面积从大到小排序
            var res = objs.OfType<Entity>().Where(o =>
            {
                if (o is Polyline poly)
                {
                    return poly.Area > 1.0;
                }
                else if (o is MPolygon mPolyggon)
                {
                    return mPolyggon.Area > 1.0;
                }
                else
                {
                    return false;
                }
            }).OrderByDescending(o =>
            {
                if (o is Polyline poly)
                {
                    return poly.Area;
                }
                else
                {
                    return (o as MPolygon).Area;
                }
            });

            return res.Count() > 0 ? res.First() : null;
        }

        public static DBObjectCollection Break(this DBObjectCollection wires, DBObjectCollection lights)
        {
            var results = new DBObjectCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lights);
            wires.OfType<Line>().ForEach(e =>
            {
                var lines = Query(spatialIndex, e);
                lines = lines.Where(l =>
                ThGeometryTool.IsCollinearEx(e.StartPoint, e.EndPoint, l.StartPoint, l.EndPoint)).ToList();
                var res = e.Difference(lines);
                res.ForEach(l => results.Add(l));
            });
            return results;
        }

        private static List<Line> Query(ThCADCoreNTSSpatialIndex spatialIndex, Line line)
        {
            var poly = ThDrawTool.ToRectangle(line.StartPoint, line.EndPoint, 2.0);
            var objs = spatialIndex.SelectCrossingPolygon(poly);
            return objs.OfType<Line>().ToList();
        }

        public static List<Line> SelectCrossingEntities(this Polyline searchFrame, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var results = new List<Line>();
            results.AddRange(spatialIndex.SelectCrossingPolygon(searchFrame).OfType<Line>());
            results.AddRange(spatialIndex.SelectFence(searchFrame).OfType<Line>());
            return results.Distinct().ToList();
        }
    }
}
