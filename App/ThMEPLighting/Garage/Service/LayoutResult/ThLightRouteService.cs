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
        private bool IsTraverseLightMidPoint = false;
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
            WireSpatialIndex = new ThCADCoreNTSSpatialIndex(Wires);
            LightSpatialIndex = new ThCADCoreNTSSpatialIndex(Lights);
            Lights.OfType<Curve>().ForEach(o => LightId.Add(o, Guid.NewGuid().ToString()));
        }
        public void Traverse()
        {
            Lights.OfType<Line>().ForEach(o =>
            {
                Traverse(o, o.StartPoint);
                Traverse(o, o.EndPoint);
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
            var outline = CreatePolyline(pt, ThGarageLightCommon.RepeatedPointDistance);
            var wires = Query(WireSpatialIndex, outline);
            if(routes.Count>0)
            {
                wires.Remove(routes.Last());
            }            
            wires = FindPortLinks(wires, pt); // 线与线的连接只能在端点
            if (wires.Count == 0)
            {
                var lights = FindLights(Query(LightSpatialIndex, outline), pt);
                lights.Remove(source.Light);
                if (lights.Count > 0)
                {
                    // 表示找到连接的灯
                    var first = lights.OfType<Curve>().First();
                    var target = ThLinkEntity.Create(FindId(first), first, pt);
                    Links.Add(ThLightLink.Create(source,target,routes));
                }
                return;
            }
            if (wires.OfType<Curve>().Where(o=> wires.Contains(o)).Any())
            {
                return;  // 路径产生自交
            }
            else
            {
                foreach (Curve wire in wires)
                {
                    var nextPt = pt.GetNextLinkPt(wire.StartPoint, wire.EndPoint);
                    var newRoutes = routes.Select(o => o).ToList();
                    newRoutes.Add(wire);
                    Traverse(nextPt, newRoutes, source);
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

        private DBObjectCollection Query(ThCADCoreNTSSpatialIndex spatialIndex, Polyline frame)
        {
            return spatialIndex
                .SelectCrossingPolygon(frame).OfType<Curve>()
                .Where(o=>o is Line || o is Arc)
                .ToCollection();
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
                    .Where(o => IsLink(o.GetMidPt(), pt))
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
        public static ThLinkEntity Create(string id, Entity light, Point3d linkPt)
        {
            return new ThLinkEntity(id, light, linkPt);
        }
    }
}
