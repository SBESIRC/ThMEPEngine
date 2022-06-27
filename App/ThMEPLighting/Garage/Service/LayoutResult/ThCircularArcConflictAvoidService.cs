using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    class ThCircularArcConflictAvoidService:IDisposable
    {
        #region ---------- 外部传入 ----------
        private double LampLength { get; set; }
        private DBObjectCollection Arcs { get; set; }
        private Dictionary<Point3d, Tuple<double,string>> LightPosDict { get; set; }
        #endregion

        #region ---------- 构建 ----------
        private const double ArcTesslateLength = 200.0; //优先用此值对圆弧分割
        private const int MinimumSegments = 20; // 分割的段数不能少于此值
        private const double SideLength = 100.0;
        private Dictionary<Arc, Polyline> ArcTesslateDict { get; set; }
        private ThCADCoreNTSSpatialIndex LightCenterSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex LightSideSpatialIndex { get; set; }
        #endregion

        public DBObjectCollection Results { get; private set; }

        public ThCircularArcConflictAvoidService(
            double lampLength,
            DBObjectCollection arcs,
            Dictionary<Point3d, Tuple<double, string>> lightPos)
        {
            Arcs = arcs;
            LightPosDict = lightPos;
            LampLength = lampLength;
            BuildArcTesslateDict();
            BuildLightSpatialIndex();
            Results = new DBObjectCollection();
        }
        public void Dispose()
        {
        }
        public void Avoid()
        {
            var conflicts = FindConflicts();
            var results = Adjust(conflicts);

            // 收集结果
            Arcs.OfType<Arc>().ForEach(o =>
            {
                if(results.ContainsKey(o))
                {
                    Results.Add(results[o]);
                }
                else
                {
                    Results.Add(o);
                }
            });
        }

        private DBObjectCollection FindConflicts()
        {
            var results = new DBObjectCollection();
            Arcs.OfType<Arc>().ForEach(o =>
            {
                if(IsConflict(o))
                {
                    results.Add(o);
                }
            });
            return results;
        }

        private Dictionary<Arc,Arc> Adjust(DBObjectCollection arcs)
        {
            var results = new Dictionary<Arc, Arc>();
            arcs.OfType<Arc>().ForEach(o =>
            {
                var newArc = ByMirror(o);
                if (newArc == null)
                {
                    newArc = ByResetArcPort(o);
                }
                if (newArc != null)
                {
                    results.Add(o, newArc);
                }
            });
            return results;
        }

        private Arc ByMirror(Arc arc)
        {
            var newArc = Mirror(arc);
            if (!IsConflict(newArc))
            {
                return newArc;
            }
            return null;
        }

        private Arc ByResetArcPort(Arc arc)
        {
            var startLight = FindLight(arc.StartPoint);
            var endLight = FindLight(arc.EndPoint);
            if(startLight==null || endLight==null )
            {
                return null;
            }
            if(startLight.Length< LampLength/2.0 || endLight.Length < LampLength / 2.0)
            {
                return null;
            }
            var referenceVec = GetArcTopVector(startLight, endLight);
            if (referenceVec.HasValue)
            {
                var firstMidPt = startLight.StartPoint.GetMidPt(startLight.EndPoint);
                var secondMidPt = endLight.StartPoint.GetMidPt(endLight.EndPoint);
                var newArc = CreateArc(firstMidPt, secondMidPt, referenceVec.Value);
                if (!IsConflict(newArc))
                {
                    return newArc;
                }
                return ByMirror(newArc);
            }
            return null;
        }

        private Arc CreateArc(Point3d start, Point3d endPt,Vector3d refenceVec)
        {
            var dis = start.DistanceTo(endPt);
            var arcAng = ThArcDrawTool.ArcAngle;
            var radius = ThArcDrawTool.CalculateRadiusByAngle(dis, arcAng);
            return ThArcDrawTool.DrawArc(start, endPt, radius, refenceVec);
        }

        private Line FindLight(Point3d pt)
        {
            var objs = Query(pt);
            return objs.OfType<Line>().Where(o => IsCloseCurvePort(pt, o)).FirstOrDefault();
        }

        private Vector3d? GetArcTopVector(Line first, Line second)
        {
            if(ThGarageUtils.IsLessThan45Degree(first.StartPoint,first.EndPoint,
                second.StartPoint,second.EndPoint))
            {
                return ThArcDrawTool.GetArcTopVector(first.StartPoint, first.EndPoint);
            }
            else
            {
                var inters = first.IntersectWithEx(second, Intersect.ExtendBoth);
                if(inters.Count>0)
                {
                    var intersPt = inters[0];
                    var firstMidPt = first.StartPoint.GetMidPt(first.EndPoint);
                    var secondMidPt = second.StartPoint.GetMidPt(second.EndPoint);
                    return ThArcDrawTool.CalculateReferenceVec(firstMidPt, secondMidPt, intersPt);
                }
            }
            return null;
        }

        private bool IsConflict(Arc arc)
        {
            var arcPoly = GetArcPolyline(arc);
            var outline = Buffer(arcPoly, 1.0);
            var lights = QueryCenters(outline);
            var sides = QuerySides(outline);
            return IsConflict(arc, lights) || IsConflict(arc, sides);
        }

        private bool IsConflict(Arc arc, DBObjectCollection lines)
        {
            return lines.OfType<Line>().Where(l => IsConflict(arc, l)).Any();
        }

        private Polyline GetArcPolyline(Arc arc)
        {
            if(ArcTesslateDict.ContainsKey(arc))
            {
                return ArcTesslateDict[arc];
            }
            return Tesslate(arc);
        }

        private bool IsConflict(Arc arc,Line line)
        {
            var pts = arc.IntersectWithEx(line);
            return pts
                .OfType<Point3d>()
                .Where(p=> !IsCloseCurvePort(p,arc))
                .Any();
        }

        private bool IsCloseCurvePort(Point3d pt, Curve curve,double tolerance=1.0)
        {
            return curve.StartPoint.DistanceTo(pt) <= tolerance ||
                curve.EndPoint.DistanceTo(pt) <= tolerance;
        }

        private void BuildArcTesslateDict()
        {
            ArcTesslateDict = new Dictionary<Arc, Polyline>();
            Arcs.OfType<Arc>().ForEach(o =>
            {
                ArcTesslateDict.Add(o, Tesslate(o));
            });
        }
        private Polyline Tesslate(Arc arc)
        {
            if(IsBiggerThan(arc.Length, ArcTesslateLength))
            {
                return arc.TessellateArcWithChord(ArcTesslateLength);
            }
            else
            {
                var splitDis = GetSplitChord(arc.Length);
                return arc.TessellateArcWithChord(splitDis);
            }
        }
        private bool IsBiggerThan(double arcLength,double arcTesslateLength)
        {
           var num = Math.Ceiling(arcLength / arcTesslateLength);
            return num >= MinimumSegments * 1.0;
        }

        private double GetSplitChord(double arcLength)
        {
            return arcLength / MinimumSegments * 1.0;
        }

        private void BuildLightSpatialIndex()
        {
            var lightObjs = BuildLights();
            var lightSideObjs = CreateSides(lightObjs);
            LightCenterSpatialIndex = new ThCADCoreNTSSpatialIndex(lightObjs);   
            LightSideSpatialIndex = new ThCADCoreNTSSpatialIndex(lightSideObjs);
        }

        private DBObjectCollection CreateSides(DBObjectCollection centers)
        {
            var results = new DBObjectCollection();
            centers.OfType<Line>().ForEach(o =>
            {
                var sides = CreateSides(o);
                results.Add(sides.Item1);
                results.Add(sides.Item2);
            });
            return results;
        }

        private Tuple<Line,Line> CreateSides(Line center)
        {
            var vec = center.LineDirection().GetPerpendicularVector();
            var spLine = CreateSide(center.StartPoint, vec);
            var epLine = CreateSide(center.EndPoint, vec);
            return Tuple.Create(spLine, epLine);
        }

        private Line CreateSide(Point3d pt, Vector3d vec)
        {
            vec = vec.GetNormal();
            var sp = pt + vec.MultiplyBy(SideLength / 2.0);
            var ep = pt - vec.MultiplyBy(SideLength / 2.0);
            return new Line(sp, ep); 
        }

        private DBObjectCollection BuildLights()
        {
            return ThBuildLightLineService.Build(LightPosDict, LampLength);
        }
        
 
        private Polyline Buffer(Polyline arc,double width)
        {
            return arc.BufferPL(width).OfType<Polyline>().OrderByDescending(o=>o.Area).First();
        }
        private Arc Mirror(Arc arc)
        {
            var clone = arc.Clone() as Arc;
            var line3d = new Line3d(arc.StartPoint,arc.EndPoint);
            var mt = Matrix3d.Mirroring(line3d);
            clone.TransformBy(mt);
            return clone;
        }
        private DBObjectCollection Query(Point3d pt,double tolerance = 1.0)
        {
            var envelope = pt.CreateSquare(tolerance);
            return QueryCenters(envelope);
        }
        private DBObjectCollection QueryCenters(Polyline arcOutline)
        {
            return LightCenterSpatialIndex.SelectCrossingPolygon(arcOutline);
        }
        private DBObjectCollection QuerySides(Polyline arcOutline)
        {
            return LightSideSpatialIndex.SelectCrossingPolygon(arcOutline);
        }
    }
}
