using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThLightLinearJumpWireFactory : LightWireFactory
    {        
        private double OffsetDis1 = 150.0; // 默认起始编号灯点连到分支线的偏移长度
        public double OffsetDis2 { get; set; } = 400.0; // 跳接线的偏移长度
        private double OffsetDis3; // 角接线的偏移长度
        private ThQueryLineService LineQuery;
        /// <summary>
        /// 收集要扣减的线段
        /// </summary>
        public List<Point3dCollection> Deductions { get; private set; }

        public ThLightLinearJumpWireFactory(List<ThLightNodeLink> lightNodeLinks):base(lightNodeLinks)
        {
            Deductions = new List<Point3dCollection>();
            LineQuery = ThQueryLineService.Create(lightNodeLinks.SelectMany(o=>o.Edges).ToList());
        }
        public override void Build()
        {
            // 初始化
            var shortenDis = LampLength / 2.0 + LampSideIntervalLength;
            OffsetDis3 = OffsetDis2;

            // 过滤两盏灯之间没有边连接(十字路口连接除外)
            LightNodeLinks = LightNodeLinks.Where(e => e.Edges.Count > 0).ToList();

            // 绘制默认编号和不在同一段上的跳接线
            //LightNodeLinks
            //    .Where(l => DefaultNumbers.Contains(l.First.Number) && !l.OnLinkPath)
            //    .ForEach(l => DrawDefaultJumpWire(l));

            // 绘制在同一段上,且不是默认编号的灯
            var samePathJumpWires = new DBObjectCollection();            
            LightNodeLinks
                .Where(l => !DefaultNumbers.Contains(l.First.Number) && l.OnLinkPath)
                .ForEach(l =>
                {
                    DrawSamePathJumpWire(l);
                    UpdateFirstLinkLine(l, shortenDis);
                    UpdateSecondLinkLine(l, shortenDis);
                    l.JumpWires.ForEach(w => samePathJumpWires.Add(w));
                });

            // 绘制拐弯的线
            var spatialIndex = new ThCADCoreNTSSpatialIndex(samePathJumpWires);
            LightNodeLinks
                .Where(l => !DefaultNumbers.Contains(l.First.Number) && !l.OnLinkPath)
                .ForEach(l =>
                {
                    DrawCornerJumpWire(l);
                    UpdateFirstLinkLine(l, shortenDis, spatialIndex);
                    UpdateSecondLinkLine(l, shortenDis, spatialIndex);
                });
        }

        public void BuildCrossLinks()
        {
            // 用于十字区域对角区域的连接
            var shortenDis = LampLength / 2.0 + LampSideIntervalLength;
            OffsetDis3 = OffsetDis2;
            // 绘制十字型连接线
            LightNodeLinks
                .Where(l => l.IsCrossLink)
                .ForEach(l =>
                {
                    DrawCrossJumpWire(l);
                });
        }

        public void BuildStraitLinks()
        {
            // 用于十字区域对角区域的连接
            var shortenDis = LampLength / 2.0 + LampSideIntervalLength;
            OffsetDis3 = OffsetDis2;
            // 绘制十字型连接线
            LightNodeLinks.ForEach(l =>
                {
                    DrawStraitJumpWire(l);
                });
        }

        private void DrawDefaultJumpWire(ThLightNodeLink lightNodeLink)
        {
            /*secondPt
                   * 
                   *  *
                   *    *
                   *      *
                   *        *
                   *          *
                   *             * 
            cornerPt* * * * * * * * *first* * * * * * 
                   *     
                   *
                   *      
             */
            var first = lightNodeLink.First.Position;
            var second = lightNodeLink.Second.Position;
            var edges = lightNodeLink.Edges;            
            // 找到第一盏灯所在的边            
            Line firstEdge = edges.Where(o => first.IsPointOnLine(o, 1.0)).First();
            // 找到拐弯的边
            Line cornerEdge =  FindBackCornerEdge(firstEdge, edges);
            // 找到拐弯的边的前一条边
            Line preEdge = edges[edges.IndexOf(cornerEdge) - 1];
            // 计算拐弯点
            var cornerPt = preEdge.FindLinkPt(cornerEdge);
            if (!cornerPt.HasValue || !IsBranchPt(cornerPt.Value))
            {
                return;
            }
            //第二盏灯在第一盏灯所在边的投影点
            var secondProjectionPt = second.GetProjectPtOnLine(
                firstEdge.StartPoint, firstEdge.EndPoint);
            //计算线的偏移方向
            var upDir = secondProjectionPt.GetVectorTo(second).GetNormal();
            var forwardDir = first.GetVectorTo(secondProjectionPt).GetNormal();
            var pt1 = first + forwardDir.MultiplyBy(LampLength / 2.0); // 获取第一点
            var pt2 = pt1 + upDir.MultiplyBy(OffsetDis1); // 获取第二点
            if(!ThGeometryTool.IsProjectionPtInLine(cornerEdge.StartPoint, cornerEdge.EndPoint, pt2))
            {
                // 如果pt2的投影不在cornerEdge里 ,调整pt2
                var midPt = cornerEdge.StartPoint.GetMidPt(cornerEdge.EndPoint);
                pt2 = midPt.GetProjectPtOnLine(pt1, pt2);
            }           
            var installPt = pt2.GetProjectPtOnLine(cornerEdge.StartPoint, cornerEdge.EndPoint);            
            //用来收集需要被扣减掉的线段
            var pts = FindPathBetweenTwoPos(installPt , cornerPt.Value, edges);

            lightNodeLink.JumpWires.Add(new Line(pt1, pt2));
            lightNodeLink.JumpWires.Add(new Line(pt2, installPt));
            Deductions.Add(pts); //收集要扣减的区域
        }

        private bool IsBranchPt(Point3d cornerPt)
        {
            var lines = LineQuery.Query(cornerPt, ThGarageLightCommon.RepeatedPointDistance);
            return lines.Count > 2;
        }
        private void DrawSamePathJumpWire(ThLightNodeLink lightNodeLink)
        {
            lightNodeLink.JumpWires = Draw(lightNodeLink);
        }

        private void DrawCornerJumpWire(ThLightNodeLink lightNodeLink)
        {
            lightNodeLink.JumpWires = Draw(lightNodeLink);
        }

        private List<Curve> Draw(ThLightNodeLink lightNodeLink)
        {
            // 连接两个灯点的边
            var path = CreatePolyline(lightNodeLink.Edges, lightNodeLink.First.Position, lightNodeLink.Second.Position);

            // 获取跳接线的偏移方向
            var offsetDir = GetJumpWireDirection(lightNodeLink);
            if (offsetDir.HasValue)
            {
                return DrawLinkCurves(path, offsetDir.Value);
            }
            else
            {
                return new List<Curve>();
            }
        }

        private Polyline CreatePolyline(List<Line> edges,Point3d first,Point3d second)
        {
            // 获取从第一个灯点到第二个灯点之间的路径
            var pts = FindPathBetweenTwoPos(first, second, edges);
            pts = pts.RemoveNeibourDuplicatedPoints();
            return pts.CreatePolyline(false);
        }

        private List<Curve> DrawLinkCurves(Polyline path,Vector3d direction)
        {
            // 对两个灯点的路径进行Buffer
            var outline = path.BufferPath(OffsetDis2);

            // 找出沿着offsetDir方向的路径
            var lines = FindLinkPath(outline, path.StartPoint, path.EndPoint, direction);
            if (lines.Count > 0)
            {
                var dir = lines[0].LineDirection();
                if (!dir.IsCodirectionalTo(direction, new Tolerance(1.0, 1.0)))
                {
                    outline = path.BufferPath(-OffsetDis2);
                    lines = FindLinkPath(outline, path.StartPoint, path.EndPoint, direction);
                }
            }
            return lines.OfType<Curve>().ToList();
        }

        private void DrawCrossJumpWire(ThLightNodeLink lightNodeLink)
        {
            var startEndPt = CalculateJumpStartEndPt(lightNodeLink);
            var startPt = startEndPt.Item1;
            var endPt = startEndPt.Item2;
            var shortRes = Shorten(startPt, endPt, LightLinkShortenDis);
            var lines = new List<Line>();
            if(CheckLightLinkConflictedSideLines(shortRes.Item1, shortRes.Item2, 1.0) == false)
            {
                lines.Add(new Line(startPt, endPt));
            }
            else
            {
                if(lightNodeLink.CrossIntersectionPt.HasValue)
                {
                    lines = DrawLinkLines(startPt, endPt, lightNodeLink.CrossIntersectionPt.Value);
                }
                else
                {
                    lines.Add(new Line(startPt, endPt));
                }
            }
            lightNodeLink.JumpWires = lines.Cast<Curve>().ToList();
        }

        private void DrawStraitJumpWire(ThLightNodeLink lightNodeLink)
        {
            var startPt = lightNodeLink.First.Position;
            var endPt = lightNodeLink.Second.Position;
            lightNodeLink.JumpWires.Add(new Line(startPt, endPt));
        }

        private List<Line> DrawLinkLines(Point3d start,Point3d end,Point3d initBrigePt)
        {
            var results = new List<Line>();
            var brigePt = FindBrigePt(start, initBrigePt);
            results.Add(new Line(start, brigePt));
            results.Add(new Line(brigePt, end));
            CrossInstallPoints.Add(brigePt);
            return results;
        }
        private List<Line> CreateContinousLines(Point3d pt,List<Vector3d> vecs)
        {
            var results = new List<Line>();
            var first = new Point3d(pt.X,pt.Y,pt.Z);
            vecs.ForEach(v =>
            {
                var second = first + v;
                results.Add(new Line(first, second));
                first = second;
            });
            return results;
        }
        private Line FindPrevCornerEdge(Line edge, List<Line> edges)
        {
            /*
             *  ------------|-----------
             *       (prev)    (back)
             */
            int index = edges.IndexOf(edge);
            for (int i = index - 1; i >= 0; i--)
            {
                Point3d preSp = edges[i].StartPoint;
                Point3d preEp = edges[i].EndPoint;
                Point3d backSp = edges[i + 1].StartPoint;
                Point3d backEp = edges[i + 1].EndPoint;                
                if (!ThGarageUtils.IsLessThan45Degree(backSp, backEp, preSp, preEp))
                {
                    return edges[i];
                }
            }
            return new Line();
        }

        private Line FindBackCornerEdge(Line edge, List<Line> edges)
        {
            /*
             *  ------------|-----------
             *       (prev)    (back)
             */
            int index = edges.IndexOf(edge);
            for (int i = index + 1; i < edges.Count; i--)
            {
                Point3d preSp = edges[i - 1].StartPoint;
                Point3d preEp = edges[i - 1].EndPoint;
                Point3d backSp = edges[i].StartPoint;
                Point3d backEp = edges[i].EndPoint;                
                if (!ThGarageUtils.IsLessThan45Degree(preSp, preEp, backSp, backEp))
                {
                    return edges[i];
                }
            }
            return new Line();
        }

        private List<Line> AdjustLightNodeLinkLine(Line first,Line second,double shortenDis)
        {
            // first 与 second 初始状态是垂直的,且有连接点
            var results = new List<Line>();            
            var linkPt = first.FindLinkPt(second);
            if(!linkPt.HasValue)
            {
                return results;
            }
            var secondFarPt = linkPt.Value.DistanceTo(second.StartPoint) < linkPt.Value.DistanceTo(second.EndPoint)
                ? second.EndPoint : second.StartPoint;
            if (second.Length > shortenDis)
            {
                var vec = linkPt.Value.GetVectorTo(secondFarPt).GetNormal();
                var shortenPt = linkPt.Value + vec.MultiplyBy(shortenDis);
                var newSecondLine = new Line(shortenPt, secondFarPt);
                var firstFarPt = linkPt.Value.DistanceTo(first.StartPoint) < linkPt.Value.DistanceTo(first.EndPoint)
                ? first.EndPoint : first.StartPoint;
                var newFirstLine = new Line(firstFarPt, shortenPt);
                // 添加顺序，新创建的第一根线，新创建的第二根线
                results.Add(newFirstLine);
                results.Add(newSecondLine);
            }
            return results;
        }
        private void UpdateFirstLinkLine(ThLightNodeLink link,double shortenDis)
        {
            if(link.JumpWires.Count<3)
            {
                return;
            }
            var lines = AdjustLightNodeLinkLine(link.JumpWires[0] as Line, link.JumpWires[1] as Line, shortenDis);
            if(lines.Count==2)
            {
                link.JumpWires.RemoveAt(0);
                link.JumpWires.RemoveAt(0);
                link.JumpWires.Insert(0, lines[1]);
                link.JumpWires.Insert(0, lines[0]);
            }
        }
        private void UpdateSecondLinkLine(ThLightNodeLink link, double shortenDis)
        {
            if (link.JumpWires.Count < 3)
            {
                return;
            }
            var lines = AdjustLightNodeLinkLine(link.JumpWires.Last() as Line
                , link.JumpWires[link.JumpWires.Count - 2] as Line, shortenDis);

            if (lines.Count == 2)
            {
                link.JumpWires.RemoveAt(link.JumpWires.Count - 1);
                link.JumpWires.RemoveAt(link.JumpWires.Count - 1);
                link.JumpWires.Add(lines[1]);
                link.JumpWires.Add(lines[0]);
            }
        }
        private void UpdateFirstLinkLine(ThLightNodeLink link, double shortenDis,ThCADCoreNTSSpatialIndex spatialIndex)
        {
            // 调整拐弯处连接
            if (link.JumpWires.Count < 3)
            {
                return;
            }
            var cornerRec = CreateCornerRec(link.JumpWires[0] as Line, link.JumpWires[1] as Line, shortenDis);
            var innerRec = Buffer(cornerRec ,-0.05 * shortenDis); // 内缩5%*shortenDis
            if(innerRec.Area<1e-4)
            {
                var dis = GetMinimumDistance(cornerRec);
                if(dis>0.0)
                {
                    innerRec = Buffer(cornerRec, -0.25 * dis);
                }
            }
            if (innerRec.Area>0.0 && spatialIndex.SelectCrossingPolygon(innerRec).Count==0)
            {
                UpdateFirstLinkLine(link, shortenDis);
            }            
        }
        private void UpdateSecondLinkLine(ThLightNodeLink link, double shortenDis, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            if (link.JumpWires.Count < 3)
            {
                return;
            }
            var cornerRec = CreateCornerRec(link.JumpWires.Last() as Line, link.JumpWires[link.JumpWires.Count - 2] as Line, shortenDis);
            var innerRec = Buffer(cornerRec, -0.05 * shortenDis); // 内缩5%*shortenDis
            if (innerRec.Area < 1e-4)
            {
                var dis = GetMinimumDistance(cornerRec);
                if (dis > 0.0)
                {
                    innerRec = Buffer(cornerRec, -0.25 * dis);
                }
            }
            if (innerRec.Area>0.0 && spatialIndex.SelectCrossingPolygon(innerRec).Count == 0)
            {
                UpdateSecondLinkLine(link, shortenDis);
            }
        }        
        private Polyline CreateCornerRec(Line first, Line second, double shortenDis)
        {
            // first 与 second 初始状态是垂直的,且有连接点
            var results = new List<Line>();
            var linkPt = first.FindLinkPt(second);
            if (!linkPt.HasValue)
            {
                return new Polyline();
            }
            var firstFarawayPt = linkPt.Value.GetNextLinkPt(first.StartPoint, first.EndPoint);
            var secondFarawayPt = linkPt.Value.GetNextLinkPt(second.StartPoint, second.EndPoint);

            var firstDir = linkPt.Value.GetVectorTo(firstFarawayPt).GetNormal();
            var secondDir = linkPt.Value.GetVectorTo(secondFarawayPt).GetNormal();

            var pt1 = linkPt.Value;
            var pt2 = pt1 + firstDir.MultiplyBy(first.Length);
            var pt3 = pt2 + secondDir.MultiplyBy(shortenDis);
            var pt4 = pt1 + secondDir.MultiplyBy(shortenDis);
            var pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };

            return pts.CreatePolyline(true);
        }
        private Polyline Buffer(Polyline poly, double dis)
        {
            var polys = poly.Buffer(dis).OfType<Polyline>().ToList();
            if(polys.Count>0)
            {
                return polys.OrderByDescending(p => p.Area).First();
            }
            else
            {
                return new Polyline();
            }            
        }
        private double GetMinimumDistance(Polyline poly)
        {
            var values = new List<double>();
            for(int i=0;i<poly.NumberOfVertices-2;i++)
            {
                var lineSeg = poly.GetLineSegmentAt(i);
                values.Add(lineSeg.Length);
            }
            values = values.OrderBy(v => v).ToList();
            values = values.Where(v => v > 0.0).ToList();
            return values.Count > 0 ? values[0] : 0.0;
        }
    }
}
