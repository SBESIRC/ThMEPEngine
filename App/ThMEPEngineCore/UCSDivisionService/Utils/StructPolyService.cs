using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.UCSDivisionService.Utils
{
    public class StructPolyService
    {
        public static List<Polyline> StructPointToPolygon(Point3dCollection points)
        {
            var ptSets = VoronoiDiagramConnect(points);
            DBObjectCollection lines = new DBObjectCollection();
            ptSets.ForEach(x =>
            {
                lines.Add(new Line(x.Item1, x.Item2));
            });
            return lines.PolygonsEx().Cast<Polyline>().ToList();
        }

        /// <summary>
        /// Connect 4 dege diagram by VoronoiDiagram way
        /// </summary>
        /// <param name="points"></param>
        public static HashSet<Tuple<Point3d, Point3d>> VoronoiDiagramConnect(Point3dCollection points, Dictionary<Polyline, Point3dCollection> poly2points = null)
        {
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); //Find a point by its surrounding line
            Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines = new Dictionary<Point3d, List<Tuple<Point3d, Point3d>>>(); //Find surrounding lines by the point in them

            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(points.ToNTSGeometry());

            //foreach (Polygon polygon in voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory).Geometries) //Same use as fallow
            foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
            {
                if (polygon.IsEmpty)
                {
                    continue;
                }
                var polyline = polygon.ToDbPolylines().First();
                foreach (Point3d pt in points)
                {
                    if (polyline.Contains(pt) && !pt2lines.ContainsKey(pt))
                    {
                        if (poly2points != null)
                        {
                            foreach (var houseBorder in poly2points.Keys)
                            {
                                if (houseBorder.Intersects(polyline))
                                {
                                    poly2points[houseBorder].Add(pt);
                                    break;
                                }
                            }
                        }

                        List<Tuple<Point3d, Point3d>> aroundLines = new List<Tuple<Point3d, Point3d>>();
                        for (int i = 0; i < polyline.NumberOfVertices - 1; ++i)
                        {
                            Tuple<Point3d, Point3d> border = new Tuple<Point3d, Point3d>(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1));
                            if (line2pt.ContainsKey(border))
                            {
                                continue;//line2pt.Add(border, new Point3d());
                            }
                            line2pt.Add(border, pt);
                            aroundLines.Add(border);
                        }
                        pt2lines.Add(pt, aroundLines.OrderByDescending(l => l.Item1.DistanceTo(l.Item2)).ToList());
                        break;
                    }
                }
            }

            HashSet<Tuple<Point3d, Point3d>> connectLines = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (Point3d pt in points)
            {
                ConnectNeighbor(pt, pt2lines, line2pt, connectLines);
            }
            HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in connectLines)
            {
                if (connectLines.Contains(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))) //judge double edge
                {
                    tuples.Add(line);
                }
            }
            return tuples;
        }

        /// <summary>
        /// Find naighbor points
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pt2lines"></param>
        /// <param name="line2pt"></param>
        /// <param name="connectLines"></param>
        public static void ConnectNeighbor(Point3d point, Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines,
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt, HashSet<Tuple<Point3d, Point3d>> connectLines)
        {
            int cnt = 0;
            if (pt2lines.ContainsKey(point))
            {
                foreach (Tuple<Point3d, Point3d> line in pt2lines[point])
                {
                    Tuple<Point3d, Point3d> conversLine = new Tuple<Point3d, Point3d>(line.Item2, line.Item1);
                    if (cnt > 3 || !line2pt.ContainsKey(conversLine))
                    {
                        break;
                    }
                    Tuple<Point3d, Point3d> connectLine = new Tuple<Point3d, Point3d>(point, line2pt[conversLine]);
                    connectLines.Add(connectLine);
                    //draw_line(point, line2pt[conversLine], 130);
                    ++cnt;
                }
            }
        }
    }
}
