using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Utls;
using ThMEPEngineCore.CAD;
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service
{
    public static class BeamExtend
    {
        public static List<BeamEdge> GetAllSides(this Polyline beamSpace, List<Line> UseBeams, List<Line> assist)
        {
            List<BeamEdge> edges = new List<BeamEdge>();
            try
            {
                var lines = beamSpace.GetAllLinesInPolyline();
                foreach (var side in lines)
                {
                    edges.AddRange(side.GetEdge(UseBeams, assist));
                }
                var index = edges.FindIndex(o => o.BeamType == BeamType.Scrap);
                if (index > 0)
                {
                    var list = edges.Take(index).ToList();
                    edges.RemoveRange(0, index);
                    edges.AddRange(list);
                }
                else if (index < 0)
                {
                    double minlength = edges.Min(o => o.BeamSide.Length);
                    index = edges.FindIndex(o => o.BeamSide.Length -minlength < 1);
                    if (index > 0)
                    {
                        var list = edges.Take(index).ToList();
                        edges.RemoveRange(0, index);
                        edges.AddRange(list);
                    }
                }
            }
            catch (Exception ex)
            {
                using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
                {
                    var objs = beamSpace.Buffer(-10);
                    if (objs.Count > 0)
                    {
                        var polyline = objs[0] as Polyline;
                        polyline.Layer = BeamConfig.ErrorLayerName;
                        polyline.ColorIndex = (int)ColorIndex.BYLAYER;
                        acad.ModelSpace.Add(polyline);
                    }
                }
                return new List<BeamEdge>();
            }
            return edges;
        }

        public static List<BeamEdge> GetEdge(this Line side, List<Line> useBeams, List<Line> assists)
        {
            List<BeamEdge> edges = new List<BeamEdge>();
            var beam = useBeams.FirstOrDefault(o => o.IsSameLine(side));
            if (!beam.IsNull())
            {
                BeamEdge beamEdge = new BeamEdge(side);
                beamEdge.BeamSide = beam;
                beamEdge.BeamType = BeamType.Beam;
                edges.Add(beamEdge);
                return edges;
            }
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                //side.ColorIndex = 2;
                //acad.ModelSpace.Add(side);
                //foreach (var item in useBeams)
                //{
                //    var a = item.Clone() as Line;
                //    a.ColorIndex = 2;
                //    acad.ModelSpace.Add(a);
                //}
            }
            var assist = assists.FirstOrDefault(o => o.IsSameLine(side));
            if (!assist.IsNull())
            {
                BeamEdge beamEdge = new BeamEdge(side);
                beamEdge.BeamSide = assist;
                beamEdge.BeamType = BeamType.Scrap;
                edges.Add(beamEdge);
                return edges;
            }

            var beams = useBeams.Union(assists).Where(o => side.IsContains(o)).ToList();
            if (beams.Count > 1)
            {
                var beamOrderly = beams.OrderBy(o => o.GetCenterPt().DistanceTo(side.StartPoint));
                foreach (var beamline in beamOrderly)
                {
                    Line sideLine = beamline.Clone() as Line;
                    if(sideLine.StartPoint.DistanceTo(side.StartPoint)>sideLine.EndPoint.DistanceTo(side.StartPoint))
                    {
                        sideLine.ReverseCurve();
                    }
                    BeamEdge beamEdge = new BeamEdge(sideLine);
                    beamEdge.BeamSide = beamline;
                    beamEdge.BeamType = useBeams.Contains(beamline) ? BeamType.Beam : BeamType.Scrap;
                    edges.Add(beamEdge);
                }
                return edges;
            }
            BeamEdge edge = new BeamEdge(side);
            edge.BeamSide = null;
            edge.BeamType = BeamType.None;
            edges.Add(edge);
            return edges;
        }

        public static bool IsSameLine(this Line line, Line otherLine, double distance = 100)
        {
            if (line == null || otherLine == null)
            {
                return false;
            }

            return (line.StartPoint.DistanceTo(otherLine.StartPoint) < distance && line.EndPoint.DistanceTo(otherLine.EndPoint) < distance)
               || (line.EndPoint.DistanceTo(otherLine.StartPoint) < distance && line.StartPoint.DistanceTo(otherLine.EndPoint) < distance);
        }

        public static bool IsContains(this Line line, Line otherLine, double distance = 10)
        {
            if (line == null || otherLine == null)
            {
                return false;
            }
            return line.DistanceTo(otherLine.StartPoint, false) < distance && line.DistanceTo(otherLine.EndPoint, false) < distance;
        }

        public static Point3d GetOnethirdPt(this Line line)
        {
            return line.StartPoint + (line.EndPoint - line.StartPoint) / 3.0;
        }

        public static Point3d GetTwothirdPt(this Line line)
        {
            return line.StartPoint + (line.EndPoint - line.StartPoint) / 3.0 * 2;
        }

        public static Point3d GetCenterPt(this Line line)
        {
            return line.StartPoint + (line.EndPoint - line.StartPoint) / 2.0;
        }

        public static double GetLineAngle(this Line line, Line line1)
        {
            double angle = Math.Abs((line.Angle - line1.Angle) / Math.PI * 180 % 180);
            return Math.Min(angle, 180 - angle);
        }

        public static double AreaRatio(this Polyline polyline1, Polyline polyline2)
        {
            return Math.Min(polyline1.Area, polyline2.Area)/Math.Max(polyline1.Area, polyline2.Area);
        }

        public static double AreaRatio(this Polyline polyline1, Polyline polyline2, Polyline polyline3)
        {
            return Math.Min(Math.Min(polyline1.Area, polyline2.Area), polyline3.Area) / Math.Max(Math.Max(polyline1.Area, polyline2.Area), polyline3.Area);
        }

        public static Polyline UnionPolygon(this List<ThBeamTopologyNode> nodes)
        {
             return nodes.Select(o => o.Boundary.Buffer(10)[0] as Polyline).ToCollection().UnionPolygons().Cast<Polyline>().OrderByDescending(x => x.Area).First().Buffer(-10)[0] as Polyline;
        }

        public static List<Polyline> UnionPolygons(this List<ThBeamTopologyNode> nodes)
        {
            return nodes.Select(o => o.Boundary.Buffer(10)[0] as Polyline).ToCollection().UnionPolygons().Cast<Polyline>().Select(o => o.Buffer(-10)[0] as Polyline).ToList();
        }

        public static List<Polyline> UnionPolygons(this List<ThBeamTopologyNode> nodes, List<Polyline> polylines)
        {
            var objs = nodes.Select(o => o.Boundary.Buffer(10)[0] as Polyline).ToCollection();
            polylines.ForEach(polyline =>
            {
                objs.Add(polyline.Buffer(10)[0] as Polyline);
            });
            return objs.UnionPolygons().Cast<Polyline>().Select(o => o.Buffer(-10)[0] as Polyline).ToList();
        }

        public static Polyline UnionPolygon(this List<ThBeamTopologyNode> nodes, Polyline polyline)
        {
            var objs = nodes.Select(o => o.Boundary.Buffer(10)[0] as Polyline).ToCollection();
            objs.Add(polyline.Buffer(10)[0] as Polyline);
            return objs.UnionPolygons().Cast<Polyline>().OrderByDescending(x => x.Area).First().Buffer(-10)[0] as Polyline;
        }

        public static Polyline UnionPolygon(this ThBeamTopologyNode node, Polyline polyline)
        {
            var temp = new DBObjectCollection
                        {
                            node.Boundary.Buffer(10)[0] as Polyline,
                            polyline.Buffer(10)[0] as Polyline,
                        };
            return temp.UnionPolygons().Cast<Polyline>().OrderByDescending(x => x.Area).First().Buffer(-10)[0] as Polyline;
        }

        public static Polyline ConvexHullPL(this Polyline polyline)
        {
            return ThCADCoreNTSPoint3dCollectionExtensions.ConvexHull(polyline.Vertices()).ToDbCollection().Cast<Polyline>().OrderByDescending(x => x.Area).First();
        }

        public static bool IsNeighbor(this List<ThBeamTopologyNode> nodes, List<ThBeamTopologyNode> node1, bool ConsiderDir = false)
        {
            if(nodes.Any(node => node.Neighbor.Any(o => node1.Contains(o.Item2))))
            {
                if (ConsiderDir)
                {
                    if(nodes.First().CheckCurrentPixel(node1.First()))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CheckCurrentPixel(this ThBeamTopologyNode currentPixel, ThBeamTopologyNode neighborCurrentPixel)
        {
            return currentPixel.LayoutLines.vector.IsParallelWithTolerance(neighborCurrentPixel.LayoutLines.vector, 25);
        }

        public static bool CheckCurrentPixelVertical(this ThBeamTopologyNode currentPixel, ThBeamTopologyNode neighborCurrentPixel)
        {
            return currentPixel.LayoutLines.vector.IsParallelWithTolerance(neighborCurrentPixel.LayoutLines.vector, 65);
        }
    }
}
