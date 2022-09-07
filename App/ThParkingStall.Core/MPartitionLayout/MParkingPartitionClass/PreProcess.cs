using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public partial class MParkingPartitionPro
    {
        /// <summary>
        /// 数据预处理
        /// </summary>
        public void PreProcess()
        {
            EliminateInvalidLanes();
            ConvertRampDatas();
            CorrectWallsWithTinyAngles();
            foreach (var lane in IniLanes)
                InitialLanes.Add(lane);
        }

        /// <summary>
        /// 如果车道线穿过建筑物了（靠近边界的情况），分割该车道线取第一段
        /// </summary>
        void EliminateInvalidLanes()
        {
            var iniLanes = IniLanes.Select(e => e.Line).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var line = IniLanes[i].Line;
                var pl = line.Buffer(DisLaneWidth / 2 - 1);
                var points = new List<Coordinate>();
                STRtree<Polygon> strTree = new STRtree<Polygon>();
                foreach (var cutter in Obstacles) strTree.Insert(cutter.EnvelopeInternal, cutter);
                var selectedGeos = strTree.Query(pl.EnvelopeInternal);
                foreach (var obj in selectedGeos)
                    points.AddRange(obj.IntersectPoint(pl));
                foreach (var obj in Obstacles)
                {
                    points.AddRange(obj.Coordinates);
                }
                points = points.Where(e => pl.Contains(e) || pl.ClosestPoint(e).Distance(e) < 0.001)
                    .Select(e => line.ClosestPoint(e)).ToList();
                var splits = SplitLine(line, points);
                for (int j = 0; j < splits.Count; j++)
                {
                    foreach (var obj in Obstacles)
                        if (obj.Contains(splits[j].MidPoint))
                        {
                            splits.RemoveAt(j);
                            j--;
                            break;
                        }
                }
                splits = splits.OrderByDescending(e => ClosestPointInCurves(e.MidPoint, Walls)).ToList();
                if (splits.Count > 0)
                {
                    var lane = splits.First();
                    IniLanes[i].Line = lane;
                }
                else
                {
                    IniLanes.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// 如果区域内含有坡道，从出入点到边界生成一条车道线
        /// </summary>
        void ConvertRampDatas()
        {
            if (RampList.Count > 0)
            {
                var ramp = RampList[0];
                var pt = ramp.InsertPt.Coordinate;
                var pl = ramp.Area;
                var seg = pl.GetEdges().OrderByDescending(t => t.Length).First();
                var vec = Vector(seg).Normalize();
                var ptest = pt.Translation(vec);
                if (pl.Contains(ptest)) vec = -vec;
                var rampline = LineSegmentSDL(pt, vec, MaxLength);
                rampline = SplitLine(rampline, IniLanes.Select(e => e.Line).ToList()).OrderBy(t => t.ClosestPoint(pt, false).Distance(pt)).First();
                var prepvec = vec.GetPerpendicularVector();
                IniLanes.Add(new Lane(rampline, prepvec));
                IniLanes.Add(new Lane(rampline, -prepvec));
                OriginalLanes.Add(rampline);
                IniLaneBoxes.Add(rampline.Buffer(DisLaneWidth / 2));
                for (int i = 0; i < IniLanes.Count; i++)
                {
                    var line = IniLanes[i].Line;
                    var nvec = IniLanes[i].Vec;
                    var intersect_points = rampline.IntersectPoint(line).ToList();
                    intersect_points = SortAlongCurve(intersect_points, line.ToLineString());
                    intersect_points = RemoveDuplicatePts(intersect_points, 1);
                    var splits = SplitLine(line, intersect_points);
                    if (splits.Count() > 1)
                    {
                        IniLanes.RemoveAt(i);
                        IniLanes.Add(new Lane(splits[0], nvec));
                        IniLanes.Add(new Lane(splits[1], nvec));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 处理倾斜角度非常小的正交墙线
        /// </summary>
        void CorrectWallsWithTinyAngles()
        {
            ProcessLanes(ref IniLanes, true);
            for (int i = 0; i < Walls.Count; i++)
            {
                for (int j = 0; j < Walls[i].Coordinates.Count() - 1; j++)
                {
                    var line = new LineSegment(Walls[i].Coordinates[j], Walls[i].Coordinates[j + 1]);
                    var anglex = Math.Abs(Vector(line).AngleTo(new Vector2D(1, 0)));
                    var angley = Math.Abs(Vector(line).AngleTo(new Vector2D(0, 1)));
                    anglex = Math.Min(anglex, Math.PI - anglex);
                    angley = Math.Min(angley, Math.PI - angley);
                    if (anglex == 0 || angley == 0) continue;
                    if (Math.Min(angley, anglex) > Math.PI / 18) continue;
                    var egdes = new Polygon(new LinearRing(line.ToLineString().Envelope.Coordinates)).GetEdges().OrderByDescending(e => e.Length).Take(2).ToList();
                    var edge = egdes[0];
                    edge = edge.Scale(ScareFactorForCollisionCheck);
                    if (edge.IntersectPoint(Boundary).Count() > 0 || !Boundary.Contains(edge.P0) || !Boundary.Contains(edge.P1))
                        edge = egdes[1];
                    else edge = egdes[0];
                    if (edge.P0.Distance(Walls[i].Coordinates[j]) < edge.P1.Distance(Walls[i].Coordinates[j]))
                    {
                        Walls[i].Coordinates[j] = edge.P0;
                        Walls[i].Coordinates[j + 1] = edge.P1;
                    }
                    else
                    {
                        Walls[i].Coordinates[j] = edge.P1;
                        Walls[i].Coordinates[j + 1] = edge.P0;
                    }
                }
            }
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i].Line;
                var splits = SplitLine(lane, Walls);
                if (splits.Count() >= 1)
                {
                    IniLanes[i].Line = splits.OrderByDescending(e => e.Length).First();
                }
            }
            try
            {
                Boundary = new Polygon(new LinearRing(JoinCurves(Walls, IniLanes.Select(e => e.Line).ToList()).OrderByDescending(e => e.Length).First().Coordinates));
            }
            catch (Exception ex) { }
        }

    }
}
