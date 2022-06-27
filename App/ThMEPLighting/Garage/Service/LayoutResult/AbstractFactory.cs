using System;
using System.Linq;
using System.Collections.Generic;
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using NFox.Cad;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public abstract class NumberTextFactory
    {
        /*            WL01
         * ---------------------------
         *             *(Gap)
         *             *
         * ---------------------------
         *             *
         *             *(Height)
         *             *
         *            灯点
        */
        protected List<ThLightEdge> LightEdges { get; set; }

        #region ---------- 文字参数(外部传入) ----------
        /// <summary>
        /// 文字偏离灯基点的高度
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        /// 文字高度
        /// </summary>
        public double TextHeight { get; set; }
        /// <summary>
        /// 宽度因子
        /// </summary>

        public double TextWidthFactor { get; set; }
        /// <summary>
        /// 文字与偏移线的间隙
        /// </summary>
        public double Gap { get; set; }
        public Dictionary<Point3d, Tuple<double, string>> LightPositionDict { get; set; }
        #endregion
        public NumberTextFactory(List<ThLightEdge> lightEdges)
        {
            Height = 400.0;
            Gap = 100.0;
            TextHeight = 300.0;
            TextWidthFactor = 0.65;
            LightEdges = lightEdges;
            LightPositionDict = new Dictionary<Point3d, Tuple<double, string>>();
        }
        public abstract DBObjectCollection Build();
        protected bool IsExisted(Point3d position, string number, double angle)
        {
            return LightPositionDict
                .Where(o => o.Key.DistanceTo(position) <= ThGarageLightCommon.RepeatedPointDistance)
                .Where(o => o.Value.Item2 == number)
                .Where(o => Math.Abs(o.Value.Item1 - angle) <= 1e-4)
                .Any();
        }
    }
    public abstract class LightWireFactory
    {
        #region ---------- 文字参数(外部传入) ----------
        /// <summary>
        /// 灯具长度
        /// </summary>
        public double LampLength { get; set; }
        /// <summary>
        /// 默认编号
        /// </summary>
        public List<string> DefaultNumbers { get; set; } = new List<string>();
        /// <summary>
        /// 灯边与连线的间隔长度
        /// </summary>
        public double LampSideIntervalLength { get; set; }
        /// <summary>
        /// 中心往两边偏移的1、2号线
        /// </summary>
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; } = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
        protected ThCADCoreNTSSpatialIndex SideLineSpatialIndex { get; set; }
        /// <summary>
        /// 灯编号的跳线要偏移的方向标记
        /// 大于0->相对车道中心线，往外偏；小于零->相对车道中心线，往里偏
        /// </summary>
        public Dictionary<string, int> DirectionConfig { get; set; } = new Dictionary<string, int>();
        #endregion
        protected double PointTolerance = 1e-6;
        protected List<ThLightNodeLink> LightNodeLinks { get; set; } = new List<ThLightNodeLink>();
        protected bool IsArcPortOnLightSide = true;
        private ThJumpWireDirectionQuery JumpWireDirectionQuery { get; set; }
        /// <summary>
        /// 用于收集在T型或十字型绘制连接线转接点
        /// </summary>
        protected List<Point3d> CrossInstallPoints { get; set; } = new List<Point3d>();
        protected double CrossInstallPtStep = 300.0;
        protected const double LinkArcTesslateLength = 500.0; 
        protected double LightLinkShortenDis = 10.0; // 用于把连接两个灯直线的线内缩，查询是否与其它灯线相交
        public LightWireFactory()
        {
            DefaultNumbers = new List<string>();
            LightNodeLinks = new List<ThLightNodeLink>();
            DirectionConfig = new Dictionary<string, int>();
            CenterSideDicts = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();  
        }
        protected LightWireFactory(List<ThLightNodeLink> lightNodeLinks)
        {
            LightNodeLinks = lightNodeLinks;
        }
        public abstract void Build();
        protected Point3dCollection FindPathBetweenTwoPos(Point3d first, Point3d installPt, List<Line> edges)
        {
            var results = new Point3dCollection();
            var firstEdge = edges.Where(o => first.IsPointOnLine(o, 1.0)).First();
            var installEdge = edges.Where(o => installPt.IsPointOnLine(o, 1.0)).First();
            var firstIndex = edges.IndexOf(firstEdge);
            var installIndex = edges.IndexOf(installEdge);
            results.Add(first);
            for (int i = firstIndex; i < installIndex; i++)
            {
                var linkPt = edges[i].FindLinkPt(edges[i + 1]);
                if (linkPt.HasValue)
                {
                    results.Add(linkPt.Value);
                }
            }
            results.Add(installPt);
            return results;
        }
        protected List<Line> FindLinkPath(Polyline outline, Point3d first, Point3d second, Vector3d refVec)
        {
            var results = new List<Line>();
            int vertices = outline.NumberOfVertices;
            int firstIndex = FindPointIndex(outline, first, PointTolerance);
            int secondIndex = FindPointIndex(outline, second, PointTolerance);
            if (firstIndex == -1 || secondIndex == -1)
            {
                return results;
            }
            if (firstIndex == secondIndex)
            {
                return results;
            }

            var forwardVec = outline.GetPoint3dAt(firstIndex).GetVectorTo(
                outline.GetPoint3dAt((firstIndex + 1) % vertices));
            int increment = forwardVec.IsParallelToEx(refVec) ? 1 : -1;
            int nextIndex = firstIndex;
            var indexList = new List<int>() { firstIndex };
            while (true)
            {
                if (increment > 0)
                {
                    nextIndex = (nextIndex + increment) % vertices;
                }
                else
                {
                    nextIndex = (nextIndex + increment + vertices) % vertices;
                }
                indexList.Add(nextIndex);
                if (nextIndex == secondIndex)
                {
                    break;
                }
            }

            if (indexList.Last() == secondIndex)
            {
                for (int i = 0; i < indexList.Count - 1; i++)
                {
                    var sp = outline.GetPoint3dAt(indexList[i]);
                    var ep = outline.GetPoint3dAt(indexList[i + 1]);
                    if (sp.DistanceTo(ep) >= 1.0)
                    {
                        // 将每段长度小于1的过滤掉
                        results.Add(new Line(sp, ep));
                    }
                }
            }
            return results;
        }
        private int FindPointIndex(Polyline poly, Point3d pt, double tolerance)
        {
            var index = -1;
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                if (poly.GetPoint3dAt(i).DistanceTo(pt) <= tolerance)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        protected Point3d CalculateJumpPortPt(Point3d position,Vector3d edgeDir,double lampLength)
        {
            return position + edgeDir.GetNormal().MultiplyBy(lampLength/2.0);
        }
        protected Vector3d GetJumpWirePortVec(Vector3d edgeDir, Vector3d lightNodeDir)
        {
            if (edgeDir.DotProduct(lightNodeDir) < 0)
            {
                edgeDir = edgeDir.Negate();
            }
            return edgeDir;
        }
        protected Vector3d? GetJumpWireDirection(ThLightNodeLink lightNodeLink)
        {
            if(CenterSideDicts.Count==0) // 视为单排布置
            {
                var firstLine = lightNodeLink.Edges.FirstOrDefault();
                var lineDir = firstLine.StartPoint.GetVectorTo(firstLine.EndPoint).GetNormal();
                var firstDir = GetJumpWireDirection(lineDir);
                if (DirectionConfig.ContainsKey(lightNodeLink.First.Number))
                {
                    var dirMark = DirectionConfig[lightNodeLink.First.Number];
                    if(dirMark==0)
                    {
                        return null;
                    }
                    else
                    {
                        return dirMark > 0 ? firstDir : firstDir.Negate();
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var firstDir = GetJumpWireDirection(lightNodeLink.First.Number, lightNodeLink.First.Position);
                if (firstDir.HasValue)
                {
                    return firstDir.Value;
                }
                else
                {
                    var secondDir = GetJumpWireDirection(lightNodeLink.Second.Number, lightNodeLink.Second.Position);
                    if (secondDir.HasValue)
                    {
                        return secondDir.Value;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        protected Vector3d? GetAdjacentJumpWireDirection(ThLightNodeLink lightNodeLink)
        {
            // 指向车道中心线的方向
            var firstDir = GetJumpWireDirection(lightNodeLink.First.Position);
            if (firstDir.HasValue)
            {
                return firstDir.Value;
            }
            else
            {
                var secondDir = GetJumpWireDirection(lightNodeLink.Second.Position);
                if (secondDir.HasValue)
                {
                    return secondDir.Value;
                }
                else
                {
                    return null;
                }
            }
        }

        private Vector3d GetJumpWireDirection(Vector3d vec)
        {
            return vec.GetAlignedDimensionTextDir();
        }
        private Vector3d? GetJumpWireDirection(string number, Point3d position)
        {
            if (JumpWireDirectionQuery == null)
            {
                JumpWireDirectionQuery = new ThJumpWireDirectionQuery(DirectionConfig, CenterSideDicts);
            }
            return JumpWireDirectionQuery.Query(number, position);
        }
        protected Vector3d? GetJumpWireDirection(Point3d position)
        {
            if (JumpWireDirectionQuery == null)
            {
                JumpWireDirectionQuery = new ThJumpWireDirectionQuery(DirectionConfig, CenterSideDicts);
            }
            return JumpWireDirectionQuery.Query(position);
        }
        protected Tuple<Point3d, Point3d> CalculateJumpStartEndPt(ThLightNodeLink lightNodeLink)
        {
            var startPt = lightNodeLink.First.Position;
            var endPt = lightNodeLink.Second.Position;
            if (IsArcPortOnLightSide)
            {
                if (lightNodeLink.Edges.Count > 0)
                {
                    var firstEdgeDir = GetJumpWirePortVec(
                        lightNodeLink.Edges.First().LineDirection(),
                        startPt.GetVectorTo(endPt).GetNormal());
                    var firstJumpPt = CalculateJumpPortPt(startPt, firstEdgeDir, LampLength);

                    var secondEdgeDir = GetJumpWirePortVec(
                        lightNodeLink.Edges.Last().LineDirection(),
                        endPt.GetVectorTo(startPt).GetNormal());
                    var secondJumpPt = CalculateJumpPortPt(endPt, secondEdgeDir, LampLength);
                    return Tuple.Create(firstJumpPt, secondJumpPt);
                }
            }
            return Tuple.Create(startPt, endPt);
        }
        protected bool IsUsed(Point3d pt)
        {
            // 检查拐角点是否被使用
            return CrossInstallPoints
                .Where(o => o.DistanceTo(pt) <= 1.0)
                .Any();
        }
        private Point3d? CalculateInstallPt(Point3d start, Point3d intersPt)
        {
            var vec = intersPt.GetVectorTo(start);
            var validPts = new List<Point3d>();
            validPts.Add(intersPt.GetExtentPoint(vec, CrossInstallPtStep));
            validPts.Add(intersPt.GetExtentPoint(vec, CrossInstallPtStep * 2.0));
            var otherPts = GetCurrentCoordPts(intersPt, CrossInstallPtStep);
            validPts.AddRange(otherPts.Where(o => intersPt.GetVectorTo(o).IsSameDirection(vec)).ToList());
            foreach (Point3d pt in validPts)
            {
                if (!IsUsed(pt))
                {
                    return pt;
                }
            }
            return null;
        }

        private Point3d? CalculateRandomInstallPt(Point3d start, Point3d intersPt, double step)
        {
            var vec = intersPt.GetVectorTo(start);
            var penpendVec = vec.GetPerpendicularVector();
            double distance = step;
            var pts = new List<Point3d>();
            while (distance <= intersPt.DistanceTo(start) / 2.0)
            {
                pts.Add(intersPt.GetExtentPoint(vec, distance));
                distance += step;
            }
            for(int i=1;i<=2;i++)
            {
                pts.Add(intersPt.GetExtentPoint(penpendVec, step*i));
                pts.Add(intersPt.GetExtentPoint(penpendVec.Negate(), step * i));
            }
            foreach (Point3d pt in pts)
            {
                if (!IsUsed(pt))
                {
                    return pt;
                }
            }
            return null;
        }

        private List<Point3d> GetCurrentCoordPts(Point3d wcsPt, double length)
        {
            var results = new List<Point3d>();
            var wcs2Ucs = AcHelper.Active.Editor.WCS2UCS();
            var ucs2Wcs = AcHelper.Active.Editor.UCS2WCS();
            var ucsPt = wcsPt.TransformBy(wcs2Ucs);
            var pt1 = ucsPt.GetExtentPoint(Vector3d.XAxis, length);
            var pt2 = ucsPt.GetExtentPoint(Vector3d.YAxis, length);
            var pt3 = ucsPt.GetExtentPoint(Vector3d.XAxis.Negate(), length);
            var pt4 = ucsPt.GetExtentPoint(Vector3d.YAxis.Negate(), length);
            results.Add(pt1.TransformBy(ucs2Wcs));
            results.Add(pt2.TransformBy(ucs2Wcs));
            results.Add(pt3.TransformBy(ucs2Wcs));
            results.Add(pt4.TransformBy(ucs2Wcs));
            return results;
        }

        protected Point3d FindBrigePt(Point3d start, Point3d initBrigePt)
        {
            var newBrigePt = initBrigePt;
            if (IsUsed(newBrigePt))
            {
                var newCornerPtRes = CalculateInstallPt(start, initBrigePt);
                if (newCornerPtRes.HasValue)
                {
                    newBrigePt = newCornerPtRes.Value;
                }
                else
                {
                    newCornerPtRes = CalculateRandomInstallPt(start, initBrigePt, CrossInstallPtStep * 0.75);
                    if (newCornerPtRes.HasValue)
                    {
                        newBrigePt = newCornerPtRes.Value;
                    }
                }
            }
            return newBrigePt;
        }
        public void BuildSideLinesSpatialIndex()
        {
            var lines = new List<Line>();
            CenterSideDicts.ForEach(o =>
            {
                lines.AddRange(o.Value.Item1);
                lines.AddRange(o.Value.Item2);
            });
            SideLineSpatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
        }
        protected DBObjectCollection QuerySideLines(Polyline envelop)
        {
            if(SideLineSpatialIndex==null)
            {
                return new DBObjectCollection();
            }
            return SideLineSpatialIndex.SelectCrossingPolygon(envelop);
        }
        protected Tuple<Point3d,Point3d> Shorten(Point3d start,Point3d end,double shortenDis)
        {
            var newSp = start.GetExtentPoint(start.GetVectorTo(end), shortenDis);
            var newEp = end.GetExtentPoint(end.GetVectorTo(start), shortenDis);
            return Tuple.Create(newSp, newEp);
        }
        protected bool CheckLightLinkConflictedSideLines(Point3d startPt, Point3d endPt, double width)
        {
            var envelop = ThDrawTool.ToOutline(startPt, endPt, width);
            return QuerySideLines(envelop).Count > 0;
        }
        protected bool CheckLightLinkConflictedSideLines(Arc arc, double width)
        {
            var polyArc = arc.TessellateArcWithArc(LinkArcTesslateLength);
            var envelop = polyArc.BufferPath(width,false);
            return QuerySideLines(envelop).Count > 0;
        }
    }
}
