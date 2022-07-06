using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThLightRouteService
    {
        public bool IsTraverseLightMidPoint { get; set; } 
        private DBObjectCollection Wires { get; set; }
        private DBObjectCollection Lights { get; set; }
        private ThCADCoreNTSSpatialIndex WireSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex LightSpatialIndex { get; set; }
        private Dictionary<Curve, string> LightId { get; set; }
        public List<ThLightLink> Links { get; private set; }
        public ThLightRouteService(
            DBObjectCollection wires,
            DBObjectCollection lights)
        {
            Wires = wires;
            Lights = lights;
            Links = new List<ThLightLink>();
            IsTraverseLightMidPoint=false;
            WireSpatialIndex = new ThCADCoreNTSSpatialIndex(Wires);
            LightSpatialIndex = new ThCADCoreNTSSpatialIndex(Lights);
            LightId = new Dictionary<Curve, string>();
            Lights.OfType<Curve>().ForEach(o => LightId.Add(o, Guid.NewGuid().ToString()));
        }
        public void Traverse()
        {
            Lights.OfType<Line>().ForEach(o =>
            {
                Traverse(o, o.StartPoint);
                Traverse(o, o.EndPoint);
                if(IsTraverseLightMidPoint)
                {
                    Traverse(o, o.GetMidPt());
                }
            });
        }
        private void Traverse(Line link,Point3d port)
        {
            var source = ThLinkEntity.Create(FindId(link), link, port);
            var links = new List<Curve>();
            Traverse(port, links, source);
        }
        private void Traverse(Point3d pt,List<Curve> routes,ThLinkEntity source)
        {
            var wires = Query(WireSpatialIndex, pt);
            if(routes.Count>0)
            {
                wires.Remove(routes.Last());
            }   
            if(routes.Count==0)
            {
                wires = FindPortLinks(wires, pt); // 线与线的连接只能在端点
            }
            if (routes.OfType<Curve>().Where(o=> wires.Contains(o)).Any())
            {
                return;  // 路径产生自交
            }
            else
            {
                foreach (Curve wire in wires)
                {
                    var nextPts = new List<Point3d>();
                    if(IsLink(pt,wire))
                    {
                        nextPts.Add(pt.GetNextLinkPt(wire.StartPoint, wire.EndPoint));
                    }
                    else
                    {
                        nextPts.Add(wire.StartPoint);
                        nextPts.Add(wire.EndPoint);
                    }
                    foreach(Point3d nextPt in nextPts)
                    {
                        var newRoutes = routes.Select(o => o).ToList();
                        newRoutes.Add(wire);
                        var lights = FindLights(Query(LightSpatialIndex, nextPt), nextPt);
                        lights.Remove(source.Light);
                        if (lights.Count > 0)
                        {
                            // 表示找到连接的灯
                            var first = lights.OfType<Curve>().First();
                            var target = ThLinkEntity.Create(FindId(first), first, nextPt);
                            Links.Add(ThLightLink.Create(source, target, newRoutes));
                            continue;
                        }
                        Traverse(nextPt, newRoutes, source);
                    }
                }
            }
        }

        private DBObjectCollection FindLights(DBObjectCollection lights,Point3d pt)
        {
            var results = new DBObjectCollection();
            results = results.Union(FindPortLinks(lights, pt));
            if(results.Count==0 && IsTraverseLightMidPoint)
            {
                results = results.Union(FindMidPtLinks(lights, pt));
            }
            return results;
        }

        private string FindId(Curve curve)
        {
            return LightId.ContainsKey(curve) ? LightId[curve] : Guid.NewGuid().ToString();
        }

        private DBObjectCollection Query(ThCADCoreNTSSpatialIndex spatialIndex, Point3d pt)
        {
            var results = new DBObjectCollection();
            var frame = CreatePolyline(pt, ThGarageLightCommon.RepeatedPointDistance);
            results = spatialIndex
                .SelectCrossingPolygon(frame).OfType<Curve>()
                .Where(o=>o is Line || o is Arc)
                .ToCollection();
            frame.Dispose();
            return results;
        }
        private Polyline CreatePolyline(Point3d pt,double width = 2.5)
        {
            return ThDrawTool.CreateSquare(pt, width);
        }
        private bool IsLink(Curve first,Curve second)
        {
            return IsLink(first.StartPoint, second.StartPoint) || 
                IsLink(first.StartPoint, second.EndPoint) ||
                IsLink(first.EndPoint, second.StartPoint) || 
                IsLink(first.EndPoint, second.EndPoint);
        }
        private bool IsLink(Curve wire, Line light,bool isLinkLightMidPoint)
        {
            if (IsLink(wire, light))
            {
                return true;
            }
            else
            {
                return isLinkLightMidPoint ? IsLink(light.GetMidPt(), wire) : false;
            }
        }
        private bool IsLink(Point3d pt, Curve curve)
        {
            return IsLink(pt, curve.StartPoint) ||
                IsLink(pt, curve.EndPoint);
        }

        private bool IsLink(Point3d first,Point3d second)
        {
            return first.DistanceTo(second) <= ThGarageLightCommon.RepeatedPointDistance;
        }
        private DBObjectCollection FindPortLinks(DBObjectCollection objs,Point3d pt)
        {
            // 返回端点与pt相连的物体
            return objs
                .OfType<Curve>()
                .Where(o => IsLink(pt, o))
                .ToCollection();
        }
        private DBObjectCollection FindMidPtLinks(DBObjectCollection objs, Point3d pt)
        {
            // 返回中点与pt相连的物体
            return objs
                    .OfType<Line>()
                    .Where(o => IsLink(o.GetMidPt(), pt) || ThGeometryTool.IsPointInLine(o.StartPoint,o.EndPoint,pt))
                    .ToCollection();
        }
    }
    class ThLightLink
    {
        public ThLinkEntity Source { get; set; }
        public ThLinkEntity Target { get; set; }
        public List<Curve> Wires { get; set; }
        private ThLightLink(ThLinkEntity source, 
            ThLinkEntity target,
            List<Curve> wires)
        {
            Source = source;
            Target = target;
            Wires = wires;
        }
        public double Length
        {
            get
            {
                return Sum();
            }
        }
        /// <summary>
        /// 连线的长度
        /// </summary>
        private double Sum()
        {
            var length = 0.0;
            foreach (Curve wire in Wires)
            {
                if (wire is Line line)
                {
                    length += line.Length;
                }
                else if (wire is Arc arc)
                {
                    length += arc.Length;
                }
            }
            return length;
        }
        private bool IsSamePath(List<Curve> firstCurves, List<Curve> secondCurves)
        {
            if (firstCurves.Count == secondCurves.Count)
            {
                int i = 0, j = 0;
                int count = firstCurves.Count;
                for (; i < count; i++)
                {
                    if (secondCurves.IndexOf(firstCurves[i]) != i)
                    {
                        break;
                    }
                }
                for (; j < count; j++)
                {
                    if (secondCurves.IndexOf(firstCurves[j]) != (count - j - 1))
                    {
                        break;
                    }
                }
                return i == count || j == count;
            }
            return false;
        }
        public bool IsEqual(ThLightLink other)
        {
            if((this.Source.IsEqual(other.Source) && this.Target.IsEqual(other.Target)) ||
                (this.Source.IsEqual(other.Target) && this.Target.IsEqual(other.Source)))
            {
                return IsSamePath(this.Wires, other.Wires);
            }
            return false;
        }
        public static ThLightLink Create(ThLinkEntity source, ThLinkEntity target, List<Curve> wires)
        {
            return new ThLightLink(source, target, wires);
        }
    }
    class ThLinkEntity
    {
        public string Id { get; set; } = "";
        public Entity Light { get; set; }
        public Point3d LinkPt { get; set; }
        private ThLinkEntity(string id,Entity light,Point3d linkPt)
        {
            Id = id;
            Light = light;
            LinkPt = linkPt;
        }
        public bool IsEqual(ThLinkEntity other)
        {
            return this.Id == other.Id;
        }
        public static ThLinkEntity Create(string id, Entity light, Point3d linkPt)
        {
            return new ThLinkEntity(id, light, linkPt);
        }
    }
}
