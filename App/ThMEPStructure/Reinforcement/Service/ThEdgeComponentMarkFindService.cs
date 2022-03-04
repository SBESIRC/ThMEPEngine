using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThEdgeComponentMarkFindService
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private Dictionary<string, DBObjectCollection> MarkLines { get; set; }
        private Dictionary<string, DBObjectCollection> MarkTexts { get; set; }
        private double CloseTolerance = 1.0; // 标注线靠近
        private double LinkLinkTolerance = 1.0; // 标注线连接点
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
        public void Find(Polyline edgeComponent)
        {
            // 获取轮廓线附近的线
            var enlarge = Buffer(edgeComponent, 1.0);
            var crossObjs = enlarge.Area > 1.0 ? Query(enlarge) : Query(edgeComponent);

            var lines = crossObjs.OfType<Line>().Where(o=>IsValid(edgeComponent,o)).ToCollection();

            lines.OfType<Line>().ForEach(l =>
            {
                var farwayPt = GetFarwayPoint(edgeComponent, l);
                var links = new List<Line> { l };
                FindLinks(links, farwayPt);
                if (links.Count == 2)
                {

                }
            });
        }

        private void FindLinks(List<Line> lines,Point3d pt)
        {
            var outline = CreateOutline(pt, 1.0);
            var links = Query(outline).OfType<Line>().ToList();
            lines.ForEach(l => links.Remove(l));

            if(links.Count==0)
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

        private void FindMarkText(Line markLine,double width)
        {
            var outline = ThDrawTool.ToRectangle(markLine.StartPoint,markLine.EndPoint,width);
            var texts = Query(outline).OfType<DBText>().Where(o=> IsValidCode(o.TextString)).ToCollection();
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


        private Polyline CreateOutline(Point3d pt,double length)
        {
            return pt.CreateSquare(length);
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
                return edgeComponent.Contains(line.EndPoint);
            }
            else if(epCloseDis <= CloseTolerance)
            {
                return edgeComponent.Contains(line.StartPoint);
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
