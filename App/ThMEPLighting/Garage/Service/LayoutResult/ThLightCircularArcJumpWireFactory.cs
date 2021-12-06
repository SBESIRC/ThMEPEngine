using System;
using DotNetARX;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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

            // 绘制十字路口跳线
            LightNodeLinks
                .Where(l => l.IsCrossLink)
                .ForEach(l => DrawCrossJumpWire(l));
        }

        private void DrawSamePathJumpWire(ThLightNodeLink lightNodeLink)
        {
            // 获取跳接线的偏移方向
            var firstLine = lightNodeLink.Edges.FirstOrDefault();
            var offsetDir = GetJumpWireDirection(lightNodeLink);
            if(!offsetDir.HasValue)
            {
                return;
            }
            var startEndPt = CalculateJumpStartEndPt(lightNodeLink);
            var startPt = startEndPt.Item1;
            var endPt = startEndPt.Item2;
            var arcTopVec = ThArcDrawTool.CalculateArcTopVec(startPt, endPt, offsetDir.Value);
            var radius = CalculateRadius(startPt.DistanceTo(endPt));
            var wire = ThArcDrawTool.DrawArc(startPt, endPt, radius, arcTopVec);
            if (wire!=null)
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
            var startEndPt = CalculateJumpStartEndPt(lightNodeLink);
            var startPt = startEndPt.Item1;
            var endPt = startEndPt.Item2;
            lightNodeLink.JumpWires = DrawInnerCornerArcs(path, startPt, endPt);
        }
        private List<Curve> DrawOutterCornerArcs(Polyline path)
        {
            throw new NotSupportedException();
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
            if(polygon.IsContains(pt1))
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

        private Polyline GetOutterOutline(Polyline path, double distance)
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

        private void DrawCrossJumpWire(ThLightNodeLink lightNodeLink)
        {
            var startEndPt = CalculateJumpStartEndPt(lightNodeLink);
            var startPt = startEndPt.Item1;
            var endPt = startEndPt.Item2;
            var offsetDir = startPt.GetVectorTo(endPt).GetPerpendicularVector();
            var arcTopVec = ThArcDrawTool.CalculateArcTopVec(startPt, endPt, offsetDir);
            //var radius = CalculateRadius(startPt.DistanceTo(endPt));
            var radius = ThArcDrawTool.CalculateRadiusByAngle(startPt.DistanceTo(endPt), ArcAngle);
            var wire = ThArcDrawTool.DrawArc(startPt, endPt, radius, arcTopVec);
            if (wire != null)
            {
                lightNodeLink.JumpWires.Add(wire);
            }
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
