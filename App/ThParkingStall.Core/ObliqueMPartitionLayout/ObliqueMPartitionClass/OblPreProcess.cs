using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public partial class ObliqueMPartition
    {
        public void PreProcess()
        {
            ConvertRampDatas();
            foreach (var lane in IniLanes)
                InitialLanes.Add(lane);
        }
        private void GetValidPartOfLaneJudgeByObstacles()
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
                    //points.AddRange(obj.IntersectPoint(pl));
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

        public void ProcessRampDatas(List<RampPro> ramps)
        {
            foreach (var ramp in ramps)
            {
                var region = ramp.Region;
                for (int i = 0; i < ramp.Points.Count; i++)
                {
                    var point = ramp.Points[i];
                    var vec = ramp.Vecs[i];
                    var test_line_from_point = LineSegmentSDL(point, vec, MaxLength);

                }
            }
        }

        private void ConvertRampDatas()
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

    }
}
