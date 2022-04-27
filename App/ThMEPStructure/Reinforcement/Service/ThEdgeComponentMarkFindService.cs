using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThEdgeComponentMarkFindService
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private Dictionary<string, DBObjectCollection> MarkLines { get; set; }
        private Dictionary<string, DBObjectCollection> MarkTexts { get; set; }
        private double CloseTolerance = 1.0; // 标注线靠近
        private double SearchMarkTextWidthTolerance = 120.0;
        private double TextParallelTolerance = 5.0; //角度
        public ThEdgeComponentMarkFindService(Dictionary<string, DBObjectCollection> markLines,
            Dictionary<string, DBObjectCollection> markTexts)
        {
            MarkLines = markLines;
            MarkTexts = markTexts;
            // 创建索引
            var objs = new DBObjectCollection();
            MarkLines.ForEach(o =>
            {
                objs = objs.Union(o.Value);
            });
            MarkTexts.ForEach(o =>
            {
                objs = objs.Union(o.Value);
            });
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public List<EdgeComponentExtractInfo> Find(Polyline edgeComponent)
        {
            var results = new List<EdgeComponentExtractInfo>();
            // 获取轮廓线附近的线
            var enlarge = Buffer(edgeComponent, 1.0);
            var crossObjs = enlarge.Area > 1.0 ? Query(enlarge) : Query(edgeComponent);
            var lines = crossObjs.OfType<Line>().Where(o=>IsValid(edgeComponent,o)).ToCollection();
            lines.OfType<Line>().ForEach(l =>
            {
                var farwayPt = GetFarwayPoint(edgeComponent, l);
                var links = new List<Line> { l };
                FindLinks(links, farwayPt);
                if (links.Count == 2 && !ThGeometryTool.IsCollinearEx(links[0].StartPoint, links[0].EndPoint, links[1].StartPoint, links[1].EndPoint))
                {
                    var codes = FindCodeTexts(links.Last(), SearchMarkTextWidthTolerance*2.0); // 找到引柱末端编号(eg. YBZ、GBZ)
                    if (codes.Count > 0)
                    {
                        // 找到多个编号，按照离
                        var linkPt = links[1].FindLinkPt(links[0]);
                        var sourcePt = farwayPt;
                        if (linkPt.Value!=null)
                        {
                            sourcePt = linkPt.Value;
                        }
                        var code = codes.OfType<DBText>().OrderBy(o => o.Position.DistanceTo(sourcePt)).First();
                        // 找到纵筋面积(配筋率)-配箍率 文字
                        var reinforceTexts = FindReinforceTexts(links.Last(), SearchMarkTextWidthTolerance);
                        if(reinforceTexts.Count>0)
                        {
                            var reinforceContent = "";
                            bool isCal = false; 
                            if (reinforceTexts.Count == 1)
                            {
                                reinforceContent = reinforceTexts.OfType<DBText>().First().TextString;
                                isCal = IsCalculationLayer(GetLayer(reinforceTexts.OfType<DBText>().First()));
                            }
                            else
                            {
                                var rangePoly = CreateRectangle(links[0], links[1]);
                                var firstFilters = reinforceTexts.OfType<DBText>().Where(o=> rangePoly.Contains(o.Position)).ToCollection();
                                if(firstFilters.Count==1)
                                {
                                    reinforceContent = reinforceTexts.OfType<DBText>().First().TextString;
                                    isCal = IsCalculationLayer(GetLayer(reinforceTexts.OfType<DBText>().First()));
                                }
                                else if(firstFilters.Count>0)
                                {
                                    // 有待进一步
                                    var codeCenter = GetCenter(code);
                                    firstFilters = firstFilters.OfType<DBText>().OrderBy(o => codeCenter.DistanceTo(GetCenter(o))).ToCollection();
                                    reinforceContent = firstFilters.OfType<DBText>().First().TextString;
                                    isCal = IsCalculationLayer(GetLayer(firstFilters.OfType<DBText>().First()));
                                }
                                rangePoly.Dispose();
                            }
                            var values = Parse(reinforceContent);
                            if(values.Count==3)
                            {
                                results.Add(CreateEdgeComponent(edgeComponent, code.TextString, values[0], values[1], values[2], isCal));
                            }
                        }
                    }
                }
            });
            return results;
        }

        private bool IsCalculationLayer(string layer)
        {
            var upper = layer.ToUpper();
            return upper.EndsWith("CAL") || upper.EndsWith("CX");
        }

        private string GetLayer(Entity entity)
        {
            foreach(var item in MarkLines)
            {
                if(item.Value.Contains(entity))
                {
                    return item.Key;
                }
            }
            foreach (var item in MarkTexts)
            {
                if (item.Value.Contains(entity))
                {
                    return item.Key;
                }
            }
            return "";
        }

        private EdgeComponentExtractInfo CreateEdgeComponent(Polyline component,string number,
            double allReinforceArea,double reinforceRatio,double stirrupRatio,bool isCalculation)
        {
            return new EdgeComponentExtractInfo
            {
                Number = number,
                EdgeComponent = component,
                AllReinforceArea = allReinforceArea,
                ReinforceRatio = reinforceRatio,
                StirrupRatio = stirrupRatio,
                IsCalculation = isCalculation
            };
        }

        private Polyline CreateRectangle(Line pre,Line next)
        {
            /*  
             *             (next)
             *        --------------
             *       /
             *      /(pre)
             *     /
             */
            var spProjectionPt = pre.StartPoint.GetProjectPtOnLine(next.StartPoint,next.EndPoint);
            var epProjectionPt = pre.EndPoint.GetProjectPtOnLine(next.StartPoint, next.EndPoint);            
            var spHeight = pre.StartPoint.DistanceTo(spProjectionPt);
            var epHeight = pre.EndPoint.DistanceTo(epProjectionPt);
            var direction = next.LineDirection().GetPerpendicularVector();
            if(spHeight> epHeight)
            {
                direction = spProjectionPt.GetVectorTo(pre.StartPoint);
            }
            else
            {
                direction = epProjectionPt.GetVectorTo(pre.EndPoint);
            }
            var maxPairs = ThGeometryTool.GetCollinearMaxPts(new List<Point3d> {
                spProjectionPt, epProjectionPt, next.StartPoint, next.EndPoint });
            var pts = new Point3dCollection();
            pts.Add(maxPairs.Item1);
            pts.Add(maxPairs.Item2);
            pts.Add(maxPairs.Item2 + direction);
            pts.Add(maxPairs.Item1 + direction);
            return pts.CreatePolyline();
        }

        private DBObjectCollection FindReinforceTexts(Line markline, double width)
        {
            var outline = CreateOutline(markline.StartPoint, markline.EndPoint, width);
            var results = FilterParallelTexts(Query(outline).OfType<DBText>().ToCollection(), markline);
            results = FilterReinforceTexts(results);
            outline.Dispose();
            return results;
        }

        private DBObjectCollection FindCodeTexts(Line markline, double width)
        {
            var outline = CreateOutline(markline.StartPoint, markline.EndPoint, width);
            var results = FilterParallelTexts(Query(outline).OfType<DBText>().ToCollection(), markline);
            results = FilterCodeTexts(results);
            outline.Dispose();
            return results;
        }

        private Point3d GetCenter(DBText dbText)
        {
            var outline = dbText.TextOBB();
            var center = outline.GetPoint3dAt(0).GetMidPt(outline.GetPoint3dAt(1));
            outline.Dispose();
            return center;
        }

        private void FindLinks(List<Line> lines,Point3d pt)
        {
            var outline = CreateOutline(pt, 1.0);
            var links = Query(outline).OfType<Line>().ToList();
            lines.ForEach(l => links.Remove(l));
            links = links.Where(o => IsLink(lines.Last(), o)).ToList(); // 是相连的
            links = links.Where(o => !IsCollinearAndOverlap(lines.Last(), o)).ToList(); // 是相连的
            if (links.Count==0)
            {
                return; 
            }
            else if (links.Count ==1)
            {
                var current = links[0];
                lines.Add(current);
                var nextPt = current.EndPoint.DistanceTo(pt) > current.StartPoint.DistanceTo(pt) ?
                    current.EndPoint : current.StartPoint;
                FindLinks(lines, nextPt);
            }
            else
            {
                return;
            }
        }

        private bool IsLink(Line pre,Line next,double tolerance =1.0)
        {
            return ThReinforcementUtils.FindLinkPt(pre, next, tolerance).HasValue;
        }

        private bool IsCollinearAndOverlap(Line pre, Line next,double tolerance=1.0)
        {
            /*
             *  --------------
             *               -------------
             *  判断两条线是否是共线且有重叠            
             */
            return ThGeometryTool.IsCollinearEx(pre.StartPoint, pre.EndPoint,
                next.StartPoint, next.EndPoint) && pre.OverlapDis(next) >= tolerance;
        }

        private DBObjectCollection FilterParallelTexts(DBObjectCollection dbTexts,Line line)
        {
            return dbTexts.OfType<DBText>().Where(o => IsCloseToParallel(o, line)).ToCollection();
        }

        private DBObjectCollection FilterCodeTexts(DBObjectCollection dbTexts)
        {
            return dbTexts.OfType<DBText>().Where(o => IsValidCode(o.TextString)).ToCollection();
        }

        private DBObjectCollection FilterReinforceTexts(DBObjectCollection dbTexts)
        {
            return dbTexts.OfType<DBText>().Where(o => IsValidReinforce(o.TextString)).ToCollection();
        }

        private bool IsCloseToParallel(DBText text,Line line)
        {
            var textAng = text.Rotation.RadToAng() % 180.0;
            var lineAng = line.Angle.RadToAng() % 180.0;
            var minus = Math.Abs(textAng - lineAng); // 0 ,180
            return minus <= TextParallelTolerance || 
                Math.Abs(minus - 180.0) <= TextParallelTolerance;
        }

        private bool IsValidCode(string code)
        {
            var newCode = code.Trim();
            if(newCode.Length>3)
            {
                var prefix = newCode.Substring(0, 3).ToUpper();
                return prefix == "YBZ" || prefix == "GBZ";
            }
            else
            {
                return false;
            }
        }

        private bool IsValidReinforce(string reinforce)
        {
            return Parse(reinforce.Trim()).Count == 3;
        }

        private List<double> Parse(string reinforceText)
        {
            // 格式1:2000.0(1.00%)-0.92%
            // 格式2:4C12(0.57%)-G(6@200)
            var results = new List<double>();
            var newReinforce = reinforceText.Replace("（", "(");
            newReinforce = newReinforce.Replace("）", ")");
            newReinforce = newReinforce.Trim();
            var firstIndex = newReinforce.IndexOf('-');
            var lastIndex = newReinforce.LastIndexOf('-');
            if(firstIndex==-1 || firstIndex != lastIndex)
            {
                return  results;
            }
            var preStr = newReinforce.Substring(0, firstIndex).Trim();
            var prePattern1 = @"^\S+\s*[(]{1}\s*\d+([.]{1}\d+)?\s*[%]+\s*[)]{1}$";
            if(!Regex.IsMatch(preStr, prePattern1))
            {
                return results;
            }
            var preIndex = preStr.IndexOf('(');
            var preStrA = preStr.Substring(0, preIndex).Trim();
            var preStrB = preStr.Substring(preIndex).Trim();
            var prePattern2 = @"^\d+(.\d+)?$";
            // 全部纵筋面积As = ?
            if (Regex.IsMatch(preStrA, prePattern2))
            {
                results.Add(double.Parse(preStrA));
            }
            else
            {
                results.Add(0.0);
            }
            // 配筋率            
            var reinforceRatios = ThReinforcementUtils.GetDoubles(preStrB);
            if(reinforceRatios.Count==1)
            {
                results.Add(reinforceRatios.First());
            }
            else
            {
                return results;
            }
            // 配箍率
            var nextStr = newReinforce.Substring(firstIndex + 1).Trim();
            var nextPattern1 = @"^\d+(.\d+)?\s*[%]{1}$";
            var nextPattern2 = @"\d+\s*[@]{1}\s*\d+";
            if (Regex.IsMatch(nextStr, nextPattern1))
            {
                var stirupRatios = ThReinforcementUtils.GetDoubles(nextStr);
                if(stirupRatios.Count==1)
                {
                    results.Add(stirupRatios.First());
                }                
            }   
            else if(Regex.IsMatch(nextStr, nextPattern2))
            {
                results.Add(0.0);
            }
            return results;
        }

        private Polyline CreateOutline(Point3d pt,double length)
        {
            return pt.CreateSquare(length);
        }

        private Polyline CreateOutline(Point3d sp, Point3d ep, double width)
        {
            return ThDrawTool.ToRectangle(sp, ep, width);
        }

        private Point3d GetFarwayPoint(Polyline edgeComponent, Line line)
        {
            var spDis = DistanceTo(edgeComponent, line.StartPoint);
            var epDis = DistanceTo(edgeComponent, line.EndPoint);
            return epDis > spDis ? line.EndPoint : line.StartPoint;
        }

        private bool IsValid(Polyline edgeComponent,Line line)
        {
            var spCloseDis = DistanceTo(edgeComponent, line.StartPoint);
            var epCloseDis = DistanceTo(edgeComponent, line.EndPoint);
            if (spCloseDis <= CloseTolerance && epCloseDis<= CloseTolerance)
            {
                // 线的两个端点都接近，也不合法
                return false;
            }
            else if(spCloseDis <= CloseTolerance)
            {
                return edgeComponent.Contains(line.EndPoint) ==false;
            }
            else if(epCloseDis <= CloseTolerance)
            {
                return edgeComponent.Contains(line.StartPoint) == false;
            }
            else
            {
                // 线的两个端点都不靠近，也不合法
                return false;
            }
        }

        private double DistanceTo(Polyline polyline, Point3d pt)
        {
            var closePt = polyline.GetClosestPointTo(pt, false);
            return pt.DistanceTo(closePt);
        }

        private DBObjectCollection Query(Polyline polygon)
        {
            return SpatialIndex.SelectCrossingPolygon(polygon);
        }
        private Polyline Buffer(Polyline polygon,double length)
        {
            var polygons = polygon.Buffer(length).OfType<Polyline>().Where(p=>p.Area>1.0).OrderByDescending(p => p.Area);
            if(polygons.Count()>0)
            {
                return polygons.First();
            }
            else
            {
                return new Polyline();
            }
        }
    }
}
