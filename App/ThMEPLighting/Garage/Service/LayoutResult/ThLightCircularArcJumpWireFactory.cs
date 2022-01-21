using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThLightCircularArcJumpWireFactory : LightWireFactory
    {
        private double ArcAngle = 0.0;
        public double Gap { get; set; } = 600.0;
        public double OffsetDistance1 { get; set; } = 150.0;
        Func<double, double, double> CalculateArcRadius;
        /// <summary>
        /// 收集要扣减的线段
        /// </summary>
        public List<Point3dCollection> Deductions { get; private set; }

        public ThLightCircularArcJumpWireFactory(List<ThLightNodeLink> lightNodeLinks):base(lightNodeLinks)
        {
            ArcAngle = ThArcDrawTool.ArcAngle;
            Deductions = new List<Point3dCollection>();
            CalculateArcRadius = ThArcDrawTool.CalculateRadiusByGap;
        }
        public override void Build()
        {
            // 过滤两盏灯之间没有边连接
            LightNodeLinks = LightNodeLinks.Where(e => e.Edges.Count > 0).ToList();

            // 绘制在同一段上,不是默认编号
            LightNodeLinks
                .Where(l => !DefaultNumbers.Contains(l.First.Number))
                .Where(l => l.OnLinkPath && l.Edges.Count>0 && !l.IsCrossLink)
                .ForEach(l => DrawSamePathJumpWire(l));

            // 绘制拐弯的线
            LightNodeLinks
                .Where(l => !DefaultNumbers.Contains(l.First.Number))
                .Where(l => !l.OnLinkPath && l.Edges.Count > 0 && !l.IsCrossLink)
                .ForEach(l => DrawCornerJumpWire(l));
        }

        public void BuildStraitLinks()
        {
            // 绘制十字路口直连跳线
            LightNodeLinks
                .ForEach(l => DrawStraitJumpWire(l));
        }

        public void BuildCrossAdjacentLinks()
        {
            // 绘制在同一段上
            LightNodeLinks
                .Where(l => l.OnLinkPath)
                .ForEach(l => DrawAdjacentSamePathJumpWire(l));
        }

        private void DrawSamePathJumpWire(ThLightNodeLink lightNodeLink)
        {
            // 获取跳接线的偏移方向            
            var offsetDir = GetJumpWireDirection(lightNodeLink);
            if(!offsetDir.HasValue)
            {
                return;
            }
            DrawArc(lightNodeLink, offsetDir.Value);
        }

        private void DrawAdjacentSamePathJumpWire(ThLightNodeLink lightNodeLink)
        {
            // 获取跳接线的偏移方向
            var offsetDir = GetAdjacentJumpWireDirection(lightNodeLink);
            if (!offsetDir.HasValue)
            {
                return;
            }
            DrawArc(lightNodeLink, offsetDir.Value);
        }

        private void DrawArc(ThLightNodeLink lightNodeLink,Vector3d direction)
        {
            var startEndPt = CalculateJumpStartEndPt(lightNodeLink);
            var startPt = startEndPt.Item1;
            var endPt = startEndPt.Item2;
            var arcTopVec = ThArcDrawTool.CalculateArcTopVec(startPt, endPt, direction);
            var radius = CalculateRadius(startPt.DistanceTo(endPt));
            var wire = ThArcDrawTool.DrawArc(startPt, endPt, radius, arcTopVec);
            if (wire != null)
            {
                lightNodeLink.JumpWires.Add(wire);
            }
        }

        private void DrawCornerJumpWire(ThLightNodeLink lightNodeLink)
        {
            // 获取从第一个灯点到第二个灯点之间的路径
            var pts = FindPathBetweenTwoPos(lightNodeLink.First.Position, lightNodeLink.Second.Position, lightNodeLink.Edges);
            if(pts.Count<2 || lightNodeLink.First.Position.DistanceTo(lightNodeLink.Second.Position)<=5.0)
            {
                return;
            }
            pts = pts.RemoveNeibourDuplicatedPoints();
            var path = pts.CreatePolyline(false);

            // 获取跳接线的偏移方向
            var offsetDir = GetJumpWireDirection(lightNodeLink);
            if (!offsetDir.HasValue)
            {
                return;
            }
            var startEndPt = CalculateJumpStartEndPt(lightNodeLink);
            var startPt = startEndPt.Item1;
            var endPt = startEndPt.Item2;

            // 获取Buffer后的轮廓线，从First到Second的路径
            var innerOutline = GetInnerOutline(path, OffsetDistance1);
            var innerDir = GetOffsetDir(path, innerOutline);
            if(offsetDir.Value.IsSameDirection(innerDir))
            {
                lightNodeLink.JumpWires = DrawInnerCornerArcs(path, startPt, endPt);
            }
            else
            {
                lightNodeLink.JumpWires = DrawOuterCornerArcs(path, startPt, endPt);
            }
        }
        private List<Curve> DrawOuterCornerArcs(Polyline path, Point3d wireSp, Point3d wireEp)
        {
            // 获取Buffer后的轮廓线，从First到Second的路径
            var outerOutline = GetOuterOutline(path, OffsetDistance1);
            var outerDir = GetOffsetDir(path, outerOutline);
            var lines = FindLinkPath(outerOutline, path.StartPoint, path.EndPoint, outerDir);
            if (lines.Count > 0)
            {
                lines.RemoveAt(0);
            }
            if (lines.Count > 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }
            var cornerPts = GetCornerPoints(lines);
            cornerPts.Insert(0, wireSp);
            cornerPts.Add(wireEp);

            var results = new List<Curve>();
            for (int i = 0; i < cornerPts.Count - 1; i++)
            {
                var arcSp = cornerPts[i];
                var arcEp = cornerPts[i + 1];
                var midPt = arcSp.GetMidPt(arcEp);
                var toPathDir = FindCloseDiretion(path, midPt);
                var arcTopVec = toPathDir.Negate();
                var radius = ThArcDrawTool.CalculateRadiusByAngle(arcSp.DistanceTo(arcEp), ArcAngle);
                var wire = ThArcDrawTool.DrawArc(arcSp, arcEp, radius, arcTopVec);
                if (wire != null)
                {
                    results.Add(wire);
                }
            }
            return results;
        }
        private List<Curve> DrawInnerCornerArcs(Polyline path,Point3d wireSp,Point3d wireEp)
        {
            // 获取Buffer后的轮廓线，从First到Second的路径
            var innerOutline = GetInnerOutline(path, OffsetDistance1);
            var innerDir = GetOffsetDir(path, innerOutline);
            var lines = FindLinkPath(innerOutline,path.StartPoint, path.EndPoint, innerDir);
            if(lines.Count>0)
            {
                lines.RemoveAt(0);
            }
            if (lines.Count > 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }
            var cornerPts = GetCornerPoints(lines);
            cornerPts.Insert(0, wireSp);
            cornerPts.Add(wireEp);

            var results = new List<Curve>();    
            for (int i = 0; i < cornerPts.Count - 1; i++)
            {
                var arcSp = cornerPts[i];
                var arcEp = cornerPts[i + 1];
                var midPt = arcSp.GetMidPt(arcEp);
                var toPathDir = FindCloseDiretion(path, midPt);
                var arcTopVec = toPathDir.Negate();
                var radius = ThArcDrawTool.CalculateRadiusByAngle(arcSp.DistanceTo(arcEp),ArcAngle);
                var wire = ThArcDrawTool.DrawArc(arcSp, arcEp, radius, arcTopVec);
                if (wire != null)
                {
                    results.Add(wire);
                }
            }
            return results;
        }        
        private Vector3d GetOffsetDir(Polyline path,Polyline polygon)
        {
            var firstSegment = path.GetLineSegmentAt(0);
            var firstMidPt = firstSegment.StartPoint.GetMidPt(firstSegment.EndPoint);
            var pendVec = firstSegment.Direction.GetPerpendicularVector().GetNormal();
            var minmumLength = GetMinimumLength(polygon);
            minmumLength = Math.Min(2.0, minmumLength);
            var pt1 = firstMidPt + pendVec.MultiplyBy(minmumLength / 2.0);
            var pt2 = firstMidPt - pendVec.MultiplyBy(minmumLength / 2.0);
            if(polygon.EntityContains(pt1))
            {
                return firstMidPt.GetVectorTo(pt1);
            }
            else
            {
                return firstMidPt.GetVectorTo(pt2);
            }
        }

        private double GetMinimumLength(Polyline polyline)
        {
            var lengthList = new List<double>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lengthList.Add(polyline.GetLineSegmentAt(i).Length);
            }
            return lengthList.Where(o => o > 1e-6).OrderBy(o => o).FirstOrDefault();
        }

        private Polyline GetInnerOutline(Polyline path,double distance)
        {
            var outline1 = path.BufferPath(distance);
            var outline2 = path.BufferPath(-distance);
            return outline1.Area < outline2.Area ? outline1 : outline2;
        }

        private Polyline GetOuterOutline(Polyline path, double distance)
        {
            var outline1 = path.BufferPath(distance);
            var outline2 = path.BufferPath(-distance);
            return outline1.Area > outline2.Area ? outline1 : outline2;
        }

        private Vector3d FindCloseDiretion(Polyline path, Point3d pt)
        {
            var closePt = path.GetClosestPointTo(pt, false);
            return pt.GetVectorTo(closePt);
        }

        private List<Point3d> GetCornerPoints(List<Line> continousLines)
        {
            var cornerPoints = new List<Point3d>();
            for(int i=0;i< continousLines.Count-1;i++)
            {
                if(!ThGarageUtils.IsLessThan45Degree(continousLines[i].StartPoint, continousLines[i].EndPoint,
                    continousLines[i+1].StartPoint, continousLines[i+1].EndPoint))
                {
                    var linkPtRes = ThGarageUtils.FindLinkPt(continousLines[i], continousLines[i + 1]);
                    if(linkPtRes.HasValue)
                    {
                        cornerPoints.Add(linkPtRes.Value);
                    }
                }
            }
            return cornerPoints;
        }

        private void DrawStraitJumpWire(ThLightNodeLink lightNodeLink)
        {
            var startPt = lightNodeLink.First.Position;
            var endPt = lightNodeLink.Second.Position;
            var direction = GetArcStraitDirection(lightNodeLink);
            var arcTopVec = ThArcDrawTool.CalculateArcTopVec(startPt, endPt, direction);
            var radius = CalculateRadius(startPt.DistanceTo(endPt));
            var wire = ThArcDrawTool.DrawArc(startPt, endPt, radius, arcTopVec);
            if (wire != null)
            {
                lightNodeLink.JumpWires.Add(wire);
            }
        }

        private Vector3d GetArcStraitDirection(ThLightNodeLink link)
        {
            if (CenterSideDicts.Count==0)
            {
                if (link.Edges.Count == 2)
                {
                    var firstDir = link.Edges[0].LineDirection();
                    var secondDir = link.Edges[1].LineDirection();
                    var firstExtent = link.First.Position + firstDir.GetPerpendicularVector().MultiplyBy(100);
                    var secondExtent = link.Second.Position + secondDir.GetPerpendicularVector().MultiplyBy(100);
                    var firstLine = new Line(link.First.Position, firstExtent);
                    var secondLine = new Line(link.Second.Position, secondExtent);
                    var pts = firstLine.IntersectWithEx(secondLine, Intersect.ExtendBoth);
                    firstLine.Dispose();
                    secondLine.Dispose();
                    if(pts.Count>0)
                    {
                        return link.First.Position.GetVectorTo(pts[0]).GetNormal();
                    }
                }
            }
            else
            {
                var firstDir = GetJumpWireDirection(link.First.Position);
                if (firstDir.HasValue)
                {
                    return firstDir.Value.Negate();
                }
                else
                {
                    var secondDir = GetJumpWireDirection(link.Second.Position);
                    if (secondDir.HasValue)
                    {
                        return secondDir.Value.Negate();
                    }
                }
            }
            return link.First.Position.GetVectorTo(link.Second.Position).GetPerpendicularVector().GetNormal();
        }

        private List<Arc> DrawLinkArcs(Point3d start, Point3d end, Point3d initBrigePt)
        {
            var results = new List<Arc>();
            var projectionPt = initBrigePt.GetProjectPtOnLine(start,end);
            var firstOffsetVec = GetOffsetVector(start, initBrigePt, end);
            var secondOffsetVec = GetOffsetVector(end, initBrigePt, start);
            var brigePt = FindBrigePt(start, initBrigePt);
            results.Add(DrawArc(start, brigePt, firstOffsetVec));
            results.Add(DrawArc(brigePt, end, secondOffsetVec));
            CrossInstallPoints.Add(brigePt);
            return results;
        }

        private Vector3d GetOffsetVector(Point3d startPt,Point3d brigePt,Point3d endPt)
        {
            if(ThGeometryTool.IsCollinearEx(startPt, brigePt, endPt))
            {
                return ThGarageUtils.GetAlignedDimensionTextDir(startPt.GetVectorTo(endPt));
            }
            else
            {
                var projectionPt = endPt.GetProjectPtOnLine(startPt, brigePt);
                return projectionPt.GetVectorTo(endPt).GetNormal();
            }
        }

        private Arc DrawArc(Point3d startPt,Point3d endPt,Vector3d topRefVec)
        {
            var arcTopVec = ThArcDrawTool.CalculateArcTopVec(startPt, endPt, topRefVec);
            var radius = ThArcDrawTool.CalculateRadiusByGap(startPt.DistanceTo(endPt), Gap);
            return ThArcDrawTool.DrawArc(startPt, endPt, radius, arcTopVec);
        }
        
        private double CalculateRadius(double lightDis)
        {
            if (CalculateArcRadius.Method.Name == "CalculateRadiusByAngle")
            {
                return CalculateArcRadius(lightDis, ArcAngle);
            }
            else if (CalculateArcRadius.Method.Name == "CalculateRadiusByGap")
            {
                return CalculateArcRadius(lightDis, Gap);
            }
            else
            {
                return 0.0;
            }
        }
    }
}
