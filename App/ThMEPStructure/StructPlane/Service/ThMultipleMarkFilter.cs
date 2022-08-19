using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThMultipleMarkFilter
    {
        /// <summary>
        /// 过滤在一个梁区域内重复标注的文字
        /// </summary>
        /// <param name="beamMarkGeos"></param>
        /// <returns></returns>
        public static List<ThGeometry> Filter(List<Dictionary<ThGeometry, List<ThGeometry>>> beamMarkGeos)
        {
            /*
             *  ---------- ---------- ---------- -----------
             *   300x200    300x200   300x200    300x200
             *  ---------------------------------------------
             */
            // 目前只支持直线
            // 一个Dict是具有共边的一组，且Key的文字内容是相同的
            var textSideDicts = new List<Dictionary<DBText, DBObjectCollection>>();
            beamMarkGeos.ForEach(o =>
            {
                var textSideDict = new Dictionary<DBText, DBObjectCollection>();
                o.ForEach(m =>
                {                    
                    var key = m.Key.Boundary as DBText;
                    var values = m.Value.Select(n => n.Boundary).ToCollection();
                    textSideDict.Add(key, values);
                });
                textSideDicts.Add(textSideDict);
            });

            var removeTexts = new DBObjectCollection();
            textSideDicts
                .Where(o=>o.Count>1) // 如果只有一个标注就不要过滤了
                .ForEach(o =>
            {
                // 把具有相邻线的文字找出来
                var beamMarks = o.Keys.ToCollection();
                var beamSides = new DBObjectCollection();
                o.ForEach(m => beamSides.AddRange(m.Value));

                if (beamMarks.OfType<DBText>().Where(k => k.GetCenterPointByOBB().DistanceTo(new Point3d(75447.7215, 55596.0808, 0)) <= 200.0).Any())
                {
                    // for debug
                }

                // 把相同梁标注的边线分成两组
                var sideGroups = new List<DBObjectCollection>();
                if(HasArc(beamSides))
                {
                    sideGroups= SplitArc(beamSides);
                    // TODO
                }
                else
                {
                    sideGroups = SplitLines(beamSides);
                    if(sideGroups.Count ==2)
                    {
                        var firstMaxPts = GetMaxPts(sideGroups[0]);
                        var secondMaxPts = GetMaxPts(sideGroups[1]);
                        var commonPart = FindCommon(firstMaxPts.Item1, firstMaxPts.Item2, secondMaxPts.Item1, secondMaxPts.Item2);
                        var commonLength = commonPart != null ? commonPart.Item1.DistanceTo(commonPart.Item2):0.0;
                        if (commonLength >= 100.0 && commonLength <= 12000.0)
                        {
                            //对于梁线大于12000mm时，不合并文字
                            //var res = Normalize(commonPart.Item1, commonPart.Item2);
                            //var sorts = beamMarks
                            //.OfType<DBText>()
                            //.OrderBy(d => d.Position.GetProjectPtOnLine(res.Item1, res.Item2)
                            //    .DistanceTo(res.Item1)).ToCollection();
                            // 保留第一个                    
                            for (int i = 1; i < beamMarks.Count; i++)
                            {
                                removeTexts.Add(beamMarks[i]);
                            }
                            // 把保留的第一个文字居中
                            Move(beamMarks.OfType<DBText>().First(), commonPart.Item1, commonPart.Item2);
                        }
                    }
                }
            });

            // 返回不要的文字Geo对象
            var removeTextDict = removeTexts.Convert();
            return beamMarkGeos
                .SelectMany(o => o.Keys.ToList())
                .Where(o => removeTextDict.ContainsKey(o.Boundary))
                .ToList();
        }

        private static Tuple<Point3d,Point3d> FindCommon(Point3d firstSp,Point3d firstEp,
            Point3d secondSp,Point3d secondEp,double tolerance=1.0)
        {
            /*
             *  ----------------------               ------------------
             *                                to
             *      ------------------------         ------------------
             */
            // 获取的公共部分在second上
            var firstSpProjectionPt = firstSp.GetProjectPtOnLine(secondSp, secondEp);
            var firstEpProjectionPt = firstEp.GetProjectPtOnLine(secondSp, secondEp);
            var secondSpProjectionPt = secondSp.GetProjectPtOnLine(firstSp, firstEp);
            var secondEpProjectionPt = secondEp.GetProjectPtOnLine(firstSp, firstEp);
            if (ThGeometryTool.IsPointOnLine(secondSp, secondEp, firstSpProjectionPt, tolerance) &&
                ThGeometryTool.IsPointOnLine(secondSp, secondEp, firstEpProjectionPt, tolerance))
            {
                // firstSp、firstEp的投影点都在secondSp个secondEp这一段上
                return Tuple.Create(firstSpProjectionPt, firstEpProjectionPt);
            }
            else if (ThGeometryTool.IsPointOnLine(firstSp, firstEp, secondSpProjectionPt, tolerance) &&
                ThGeometryTool.IsPointOnLine(firstSp, firstEp, secondEpProjectionPt, tolerance))
            {
                // secondSp、secondEp在first上的投影点都在firstSp和firstEp这一段上
                return Tuple.Create(secondSpProjectionPt, secondEpProjectionPt);
            }
            else if(ThGeometryTool.IsPointOnLine(secondSp, secondEp, firstSpProjectionPt, tolerance))
            {
                // firstSp的投影点都在secondSp个secondEp这一段上
                if(ThGeometryTool.IsPointOnLine(firstSpProjectionPt, firstEpProjectionPt, secondEp, tolerance))
                {
                    return Tuple.Create(firstSpProjectionPt, secondEp);
                }
                else
                {
                    return Tuple.Create(firstSpProjectionPt, secondSp);
                }
            }
            else if(ThGeometryTool.IsPointOnLine(secondSp, secondEp, firstEpProjectionPt, tolerance))
            {
                // firstEp的投影点都在secondSp个secondEp这一段上
                if (ThGeometryTool.IsPointOnLine(firstSpProjectionPt, firstEpProjectionPt, secondEp, tolerance))
                {
                    return Tuple.Create(firstEpProjectionPt, secondEp);
                }
                else
                {
                    return Tuple.Create(firstEpProjectionPt, secondSp);
                }
            }            
            else
            {
                // 没有公共部分
                return null;
            }
        }

        private static Tuple<Point3d,Point3d> GetMaxPts(DBObjectCollection colliearLines)
        {
            var pts = new List<Point3d>();
            colliearLines.OfType<Line>().ForEach(l =>
            {
                pts.Add(l.StartPoint);
                pts.Add(l.EndPoint);
            });
            return pts.GetCollinearMaxPts();
        }


        private static bool HasArc(DBObjectCollection curves)
        {
            return curves.OfType<Arc>().Any();
        }

        private static List<DBObjectCollection> SplitArc(DBObjectCollection arcs)
        {
            throw new NotImplementedException();
        }

        private static List<DBObjectCollection> SplitLines(DBObjectCollection lines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            var length = lines.OfType<Line>().Sum(l=>l.Length);
            var groups = new List<DBObjectCollection>();
            lines.OfType<Line>().ForEach(l => 
            {
                if (!groups.Where(o =>o.IsContains(l)).Any())
                {
                    var midPt = l.StartPoint.GetMidPt(l.EndPoint);
                    var lineDir = l.StartPoint.GetVectorTo(l.EndPoint);
                    var envelop = midPt.CreateRectangle(lineDir, length, 2.0);
                    groups.Add(spatialIndex.SelectCrossingPolygon(envelop));
                    envelop.Dispose();
                }
            });
            return groups;
        }

        private static void Move(DBText text, Point3d lineSp, Point3d lineEp)
        {
            var center = text.GetCenterPointByOBB();
            var projectionpt = center.GetProjectPtOnLine(lineSp, lineEp);
            var midPt = lineSp.GetMidPt(lineEp);
            var mt = Matrix3d.Displacement(projectionpt.GetVectorTo(midPt));
            text.TransformBy(mt);
        }

        private static Tuple<Point3d, Point3d> Normalize(Point3d lineSp,Point3d lineEp)
        {
            if(lineSp.X< lineEp.X)
            {
                return Tuple.Create(lineSp, lineEp);
            }
            else if(lineEp.X < lineSp.X)
            {
                return Tuple.Create(lineEp, lineSp);
            }
            else
            {
                if(lineSp.Y< lineEp.Y)
                {
                    return Tuple.Create(lineSp, lineEp);
                }
                else
                {
                    return Tuple.Create(lineEp, lineSp);
                }
            }
        }

        private static Dictionary<Line,DBObjectCollection> GetMatchedDbTexts(
            Dictionary<DBText, DBObjectCollection> beamSideLines)
        {
            var results = new Dictionary<Line, DBObjectCollection>();
            var totalBeamLines = new DBObjectCollection();
            beamSideLines.ForEach(o => totalBeamLines.AddRange(o.Value));
            totalBeamLines = totalBeamLines.DistinctEx();
            totalBeamLines.OfType<Line>();
            totalBeamLines.OfType<Line>()
                .OrderByDescending(o => o.Length)
                .ForEach(l =>
            {
                var texts = new DBObjectCollection();
                foreach(var item in beamSideLines)
                {
                    if(item.Value.Contains(l))
                    {
                        if(!results.Where(o=>o.Value.Contains(item.Key)).Any())
                        {
                            // 如果此文字已会属于别的线，不要再加入
                            texts.Add(item.Key);
                        }                        
                    }
                }
                results.Add(l, texts);
            });
            return results;
        }
    }
}
