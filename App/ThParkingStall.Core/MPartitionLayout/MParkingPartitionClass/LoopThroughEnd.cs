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
        /// 将尽端环通的车道线在能连通的地方连同为一条
        /// </summary>
        private void JoinLoopThroughLanes()
        {
            var loopLanes = IniLanes.Where(e => e.IsGeneratedForLoopThrough || e.IsAdjLaneForProcessLoopThroughEnd).ToList();
            IniLanes = IniLanes.Except(loopLanes).ToList();
            if (loopLanes.Count >= 2)
            {
                for (int i = 0; i < loopLanes.Count - 1; i++)
                {
                    for (int j = i + 1; j < loopLanes.Count; j++)
                    {
                        var existLane = loopLanes[i];
                        var lane = loopLanes[j];
                        if (existLane.Vec.IsParallel(lane.Vec) && existLane.Vec.Dot(lane.Vec) > 0)
                        {
                            var tol = 1;
                            if (existLane.Line.P0.Distance(lane.Line.P0) < tol || existLane.Line.P0.Distance(lane.Line.P1) < tol
                                || existLane.Line.P1.Distance(lane.Line.P0) < tol || existLane.Line.P1.Distance(lane.Line.P1) < tol)
                            {
                                var line_a = existLane.Line;
                                var line_b = lane.Line;
                                var linstrings = JoinCurves(new List<LineString>(), new List<LineSegment>() { line_a, line_b }).First();
                                var line = new LineSegment(linstrings.StartPoint.Coordinate, linstrings.EndPoint.Coordinate);
                                loopLanes[i].Line=line;
                                loopLanes.RemoveAt(j);
                                j--;
                            }
                        }
                    }
                }
            }
            IniLanes.AddRange(loopLanes);
        }

        /// <summary>
        /// 剔除因生成环通车道而导致的原始车道线及模块生成的无效区域
        /// </summary>
        private void ThinInvalidLanesAndSpaceForNewLoopThroughLanes()
        {
            var dis_singleModule_depth = DisCarAndHalfLane + CollisionD - CollisionTOP;
            var loopLanes = IniLanes.Where(e => e.IsGeneratedForLoopThrough || e.IsAdjLaneForProcessLoopThroughEnd).ToList();
            foreach (var lane in loopLanes)
            {
                var rec = PolyFromLines(lane.Line, lane.Line.Translation(lane.Vec.Normalize() * dis_singleModule_depth));
                var rec_sc = rec.Scale(ScareFactorForCollisionCheck);
                for (int i = 0; i < IniLanes.Count; i++)
                {
                    if (/*IniLanes[i].Line.ToLineString().Intersects(rec_sc)*/PolyFromLines(IniLanes[i].Line, IniLanes[i].Line.Translation(IniLanes[i].Vec.Normalize()*100)).Intersects(rec_sc))
                    {
                        var splits = SplitLine(IniLanes[i].Line, rec).Where(e => !e.ToLineString().Intersects(rec_sc));
                        if (splits.Count() >0)
                        {
                            if(splits.Count()==2)
                                IniLanes[i].Line = splits.First();
                            else if(splits.Count()==1)
                                IniLanes[i].Line = splits.First();
                        }
                        else
                        {
                            IniLanes.RemoveAt(i);
                            i--;
                        }
                    }
                }
                var crossedBox = CarBoxes.Where(e => e.Intersects(rec_sc));
                if (CarBoxes.Count() > 0)
                {
                    CarBoxes = CarBoxes.Except(crossedBox).ToList();
                    crossedBox = crossedBox.Select(e =>
                     {
                         var splits = SplitCurve(e, rec).Where(t => !t.Intersects(rec_sc));
                         if (splits.Count() > 0)
                         {
                             var coords=splits.First().Coordinates;
                             var pl = PolyFromPoints(coords.ToList());
                             return pl;
                         }
                         return new Polygon(new LinearRing(new Coordinate[0]));
                     });
                    CarBoxes.AddRange(crossedBox);
                    CarBoxesSpatialIndex = new MNTSSpatialIndex(CarBoxes);
                }
                var crossedBoxPlus = CarBoxesPlus.Where(e => e.Box!=null && e.Box.Intersects(rec_sc));
                if (crossedBoxPlus.Count() > 0)
                {
                    CarBoxesPlus = CarBoxesPlus.Except(crossedBoxPlus).ToList();
                    crossedBoxPlus = crossedBoxPlus.Select(e =>
                     {
                         var splits = SplitCurve(e.Box, rec).Where(t => !t.Intersects(rec_sc));
                         if (splits.Count() > 0)
                         {
                             var coords = splits.First().Coordinates;
                             var pl = PolyFromPoints(coords.ToList());
                             e.Box = pl;
                             return e;
                         }
                         return new CarBoxPlus();
                     });
                    CarBoxesPlus.AddRange(crossedBoxPlus);
                }
                for (int i = 0; i < CarModules.Count; i++)
                {
                    var module=CarModules[i];
                    if (/*module.Line.ToLineString().Intersects(rec_sc)*/PolyFromLines(module.Line, module.Line.Translation(module.Vec.Normalize() * 100)).Intersects(rec_sc))
                    {
                        var splits = SplitLine(module.Line, rec).Where(e => !e.ToLineString().Intersects(rec_sc));
                        if (splits.Count() > 0)
                        {
                            CarModules[i].Line=splits.First();
                        }
                        else
                        {
                            CarModules.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                    if (module.Box.Intersects(rec_sc))
                    {
                        var splits = SplitCurve(module.Box, rec).Where(t => !t.Intersects(rec_sc));
                        if (splits.Count() > 0)
                        {
                            var coords = splits.First().Coordinates;
                            var pl = PolyFromPoints(coords.ToList());
                            CarModules[i].Box=pl;
                        }
                        else
                        {
                            CarModules.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }

    }
}
