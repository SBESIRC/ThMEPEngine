﻿using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public partial class MParkingPartitionPro
    {
        public static void SortLaneByDirection(List<Lane> lanes, int mode)
        {
            var comparer = new LaneComparer(mode);
            lanes.Sort(comparer);
        }
        private class LaneComparer : IComparer<Lane>
        {
            public LaneComparer(int mode)
            {
                Mode = mode;
            }
            private int Mode;
            public int Compare(Lane a, Lane b)
            {
                if (Mode == 0)
                {
                    return CompareLength(a.Line, b.Line);
                }
                else if (Mode == 1)
                {
                    if (IsHorizontalLine(a.Line) && !IsHorizontalLine(b.Line)) return -1;
                    else if (!IsHorizontalLine(a.Line) && IsHorizontalLine(b.Line)) return 1;
                    else
                    {
                        return CompareLength(a.Line, b.Line);
                    }
                }
                else if (Mode == 2)
                {
                    if (IsVerticalLine(a.Line) && !IsVerticalLine(b.Line)) return -1;
                    else if (!IsVerticalLine(a.Line) && IsVerticalLine(b.Line)) return 1;
                    else
                    {
                        return CompareLength(a.Line, b.Line);
                    }
                }
                else return 0;
            }
            private int CompareLength(LineSegment a, LineSegment b)
            {
                if (a.Length > b.Length) return -1;
                else if (a.Length < b.Length) return 1;
                return 0;
            }
        }
        private List<LineSegment> SplitBufferLineByPoly(LineSegment line, double distance, Polygon cutter)
        {
            var pl = line.Buffer(distance);
            var splits = SplitCurve(cutter, pl);
            if (splits.Count() == 1)
            {
                return new List<LineSegment> { line };
            }
            splits = splits.Where(e => pl.Contains(e.GetMidPoint())).ToArray();
            var points = new List<Coordinate>();
            foreach (var crv in splits)
                points.AddRange(crv.Coordinates);
            points = points.Select(p => line.ClosestPoint(p)).ToList();
            return SplitLine(line, points);
        }
        private List<LineSegment> SplitLineBySpacialIndexInPoly(LineSegment line, Polygon polyline, MNTSSpatialIndex spatialIndex, bool allow_on_edge = true)
        {
            var crossed = spatialIndex.SelectCrossingGeometry(polyline).Cast<Polygon>();
            crossed = crossed.Where(e => Boundary.Contains(e.Envelope.Centroid) || Boundary.IntersectPoint(e).Count() > 0);
            var points = new List<Coordinate>();
            foreach (var c in crossed)
            {
                points.AddRange(c.Coordinates);
                points.AddRange(c.IntersectPoint(polyline));
            }
            points = points.Where(p =>
            {
                var conda = polyline.Contains(p);
                if (!allow_on_edge) conda = conda || polyline.ClosestPoint(p).Distance(p) < 1;
                return conda;
            }).Select(p => line.ClosestPoint(p)).ToList();
            return SplitLine(line, points);
        }
        private bool IsConnectedToLane(LineSegment line)
        {
            if (ClosestPointInVertLines(line.P0, line, IniLanes.Select(e => e.Line)) < 10
                || ClosestPointInVertLines(line.P1, line, IniLanes.Select(e => e.Line)) < 10)
                return true;
            else return false;
        }
        private bool IsConnectedToLane(LineSegment line, bool Startpoint)
        {
            if (Startpoint)
            {
                if (ClosestPointInVertLines(line.P0, line, IniLanes.Select(e => e.Line)) < 10) return true;
                else return false;
            }
            else
            {
                if (ClosestPointInVertLines(line.P1, line, IniLanes.Select(e => e.Line)) < 10) return true;
                else return false;
            }
        }
        private bool HasParallelLaneForwardExisted(LineSegment line, Vector2D vec, double maxlength, double minlength, ref double dis_to_move
           , ref LineSegment prepLine)
        {
            var lperp = LineSegmentSDL(line.MidPoint.Translation(vec * 100), vec, maxlength);
            var lins = IniLanes.Where(e => IsParallelLine(line, e.Line))
                .Where(e => e.Line.IntersectPoint(lperp).Count() > 0)
                .Where(e => e.Line.Length > line.Length / 3)
                .OrderBy(e => line.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint))
                .Select(e => e.Line);
            lins = lins.Where(e => e.ClosestPoint(line.MidPoint).Distance(line.MidPoint) > minlength);
            if (lins.Count() == 0) return false;
            else
            {
                var lin = lins.First();
                var dis1 = lin.ClosestPoint(line.P0)
                    .Distance(lin.ClosestPoint(line.P0));
                var dis2 = lin.ClosestPoint(line.P1)
                    .Distance(lin.ClosestPoint(line.P1));
                if (dis1 + dis2 < line.Length / 2)
                {
                    dis_to_move = lin.ClosestPoint(line.MidPoint).Distance(line.MidPoint);
                    prepLine = lperp;
                    return true;
                }
                else return false;
            }
        }
        private double IsEssentialToCloseToBuilding(LineSegment line, Vector2D vec)
        {
            if (!GenerateLaneForLayoutingCarsInShearWall) return -1;
            if (!IsPerpVector(vec, Vector2D.Create(1, 0))) return -1;
            if (vec.Y < 0) return -1;
            var bf = line.Buffer(DisLaneWidth / 2);
            if (ObstaclesSpatialIndex.SelectCrossingGeometry(bf).Count > 0)
            {
                return -1;
            }
            var linetest = new LineSegment(line);
            linetest=linetest.Translation(vec * MaxLength);
            var pl = PolyFromLines(line, linetest);
            var points = new List<Coordinate>();
            points = ObstacleVertexes.Where(e => pl.IsPointInFast(e)).OrderBy(e => line.ClosestPoint(e).Distance(e)).ToList();
            if (points.Count() < 1) return -1;
            var crossedplys = ObstaclesSpatialIndex.SelectCrossingGeometry(pl).Cast<Polygon>();
            double crossedlength = 0;
            foreach (var c in crossedplys)
            {
                var crossededges = SplitCurve(c, pl).Where(e => pl.IsPointInFast(e.GetMidPoint()));
                foreach (var ed in crossededges) crossedlength += ed.Length;
            }
            if (crossedlength < 10000) return -1;
            var ltest_ob_near_boundary = LineSegmentSDL(points.First(), vec, DisVertCarLength);
            if (ltest_ob_near_boundary.IntersectPoint(Boundary).Count() > 0) return -1;
            var dist = line.ClosestPoint(points.First()).Distance(points.First());
            var lperp = LineSegmentSDL(line.MidPoint.Translation(vec * 100), vec, dist + 1);
            var lanes = IniLanes.Where(e => IsParallelLine(e.Line, line))
                .Where(e => e.Line.IntersectPoint(lperp).Count() > 0);
            if (lanes.Count() > 0) return -1;
            var lt = new LineSegment(line);
            lt=lt.Translation(vec * dist);
            var ltbf = lt.Buffer(DisLaneWidth / 2);
            points = points.Where(e => ltbf.IsPointInFast(e)).OrderBy(e => e.X).ToList();
            if (points.Count() < 1) return -1;
            var length = points.Last().X - points.First().X;
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            int offsetcount = 0;
            bool isvalid = false;
            lt=lt.Translation(-vec * DisLaneWidth / 2);
            int cyclecount = 5;
            var moduledist = (dist - DisLaneWidth / 2) % DisModulus;
            if (moduledist > 0 && moduledist <= DisVertCarLength)
            {
                dist = dist - DisLaneWidth / 2;
                dist = dist - moduledist;
                return dist;
            }
            for (int i = 0; i < cyclecount; i++)
            {
                var vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, new Lane(lt, vec.Normalize()));
                double validlength = 0;
                vertlanes.ForEach(e => validlength += e.Line.Length);
                if (validlength >= length / 2)
                {
                    isvalid = true;
                    break;
                }
                offsetcount++;
                lt=lt.Translation(-vec.Normalize() * (DisVertCarLength / cyclecount));
            }
            if (isvalid)
            {
                dist = dist - DisLaneWidth / 2;
                dist = dist - offsetcount * (DisVertCarLength / cyclecount);
                return dist;
            }
            return -1;
        }
        private void UpdateLaneBoxAndSpatialIndexForGenerateVertLanes()
        {
            LaneSpatialIndex.Update(IniLanes.Select(e => (Geometry)PolyFromLine(e.Line)).ToList(), new List<Geometry>());
            LaneBoxes.AddRange(IniLanes.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = new LineSegment(e.Line);
                var lb = new LineSegment(e.Line);
                la=la.Translation(Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                lb=lb.Translation(-Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                var py = PolyFromLines(la, lb);
                return py;
            }));
            LaneBoxes.AddRange(CarModules.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = new LineSegment(e.Line);
                var lb = new LineSegment(e.Line);
                la=la.Translation(Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                lb=lb.Translation(-Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                var py = PolyFromLines(la, lb);
                return py;
            }));
            LaneBufferSpatialIndex.Update(LaneBoxes.Cast<Geometry>().ToList(), new List<Geometry>());
        }
        public List<Lane> GeneratePerpModuleLanes(double mindistance, double minlength, bool judge_cross_carbox = true
        , Lane specialLane = null)
        {
            var lanes = new List<Lane>();
            var Lanes = IniLanes;
            if (specialLane != null)
                Lanes = new List<Lane>() { specialLane };
            foreach (var lane in Lanes)
            {
                var line = new LineSegment(lane.Line);
                var linetest = new LineSegment(line);
                linetest=linetest.Translation(lane.Vec.Normalize() * mindistance);
                var bdpl = PolyFromLines(line, linetest);
                bdpl=bdpl.Scale( ScareFactorForCollisionCheck);
                var bdpoints = Boundary.Coordinates.ToList();
                bdpoints.AddRange(Boundary.IntersectPoint(bdpl));
                bdpl=bdpl.Scale( 1 / (ScareFactorForCollisionCheck - 0.01));
                bdpoints = bdpoints.Where(p => bdpl.IsPointInFast(p)).Select(p => linetest.ClosestPoint(p)).ToList();
                var bdsplits = SplitLine(linetest, bdpoints).Where(e => Boundary.Contains(e.MidPoint) || Boundary.ClosestPoint(e.MidPoint).Distance(e.MidPoint) < 1).Where(e => e.Length >= minlength);
                foreach (var bsplit in bdsplits)
                {
                    var bdsplit = bsplit;
                    bdsplit =bdsplit.Translation(-lane.Vec.Normalize() * mindistance);
                    var bdsplittest = new LineSegment(bdsplit);
                    bdsplittest=bdsplittest.Translation(lane.Vec.Normalize() * mindistance);
                    var obpl = PolyFromLines(bdsplit, bdsplittest);
                    obpl=obpl.Scale( ScareFactorForCollisionCheck);
                    var obcrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(obpl).Cast<Polygon>().ToList();
                    var obpoints = new List<Coordinate>();
                    foreach (var cross in obcrossed)
                    {
                        obpoints.AddRange(cross.Coordinates);
                        obpoints.AddRange(cross.IntersectPoint(obpl));
                    }
                    obpl=obpl.Scale( 1 / (ScareFactorForCollisionCheck - 0.01));
                    obpoints = obpoints.Where(p => obpl.IsPointInFast(p)).Select(p => bdsplittest.ClosestPoint(p)).ToList();
                    var boxsplits = SplitLine(bdsplittest, obpoints).Where(e => !IsInAnyPolys(e.MidPoint, obcrossed)).Where(e => e.Length >= minlength);
                    foreach (var bxsplit in boxsplits)
                    {
                        var boxsplit = bxsplit;
                        boxsplit=boxsplit.Translation(-lane.Vec.Normalize() * mindistance);
                        var boxsplittest = new LineSegment(boxsplit);
                        boxsplittest=boxsplittest.Translation(lane.Vec.Normalize() * mindistance);
                        var boxpl = PolyFromLines(boxsplit, boxsplittest);
                        boxpl=boxpl.Scale( ScareFactorForCollisionCheck);
                        var boxcrossed = new List<Polygon>();
                        if (judge_cross_carbox)
                            boxcrossed = CarBoxesSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>().ToList();
                        else
                            boxcrossed = CarSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>().ToList();
                        boxpl=boxpl.Translation(lane.Vec.Normalize() * DisLaneWidth / 2);
                        //boxsplittest.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * DisLaneWidth / 2));
                        //boxpl = CreatPolyFromLines(boxsplit, boxsplittest);
                        //boxpl.Scale(boxpl.GetRecCentroid(), ScareFactorForCollisionCheck);
                        boxcrossed.AddRange(LaneSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>());
                        //boxcrossed.AddRange(LaneBufferSpatialIndex.SelectCrossingPolygon(boxpl).Cast<Polyline>());
                        foreach (var bx in LaneBufferSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>())
                        {
                            var pts = bx.Coordinates.ToList();
                            var pta = AveragePoint(pts[0], pts[3]);
                            var ptb = AveragePoint(pts[1], pts[2]);
                            var lin = new LineSegment(pta, ptb);
                            boxcrossed.Add(lin.Buffer(DisLaneWidth / 2));
                        }
                        boxsplittest=boxsplittest.Translation(lane.Vec.Normalize() * DisLaneWidth / 2);
                        var boxpoints = new List<Coordinate>();
                        foreach (var cross in boxcrossed)
                        {
                            boxpoints.AddRange(cross.Coordinates);
                            boxpoints.AddRange(cross.IntersectPoint(boxpl));
                        }
                        boxpl=boxpl.Scale(1 / (ScareFactorForCollisionCheck - 0.01));
                        boxpoints = boxpoints.Where(p => boxpl.IsPointInFast(p)).Select(p => boxsplittest.ClosestPoint(p)).ToList();
                        var splits = SplitLine(boxsplittest, boxpoints).Where(e => e.Length >= minlength)
                            .Where(e =>
                            {
                                var k = new LineSegment(e);
                                k=k.Translation(-lane.Vec.Normalize() * DisLaneWidth / 2);
                                var con = !IsInAnyBoxes(k.MidPoint, LaneBoxes, true);
                                if (con) return true;
                                else return false;
                            });
                        if (judge_cross_carbox)
                        {
                            splits = splits.Where(e => !IsInAnyBoxes(e.MidPoint, CarBoxes, true));
                        }
                        splits = splits.Where(e =>
                        {
                            var tlt = new LineSegment(boxsplit.ClosestPoint(e.P0), boxsplit.ClosestPoint(e.P1));
                            var plt = PolyFromLines(tlt, e);
                            foreach (var il in boxcrossed)
                            {
                                if (plt.Contains(il.Centroid)) return false;
                            }
                            return true;
                        });
                        foreach (var slit in splits)
                        {
                            var split = slit;
                            split=split.Translation(-lane.Vec.Normalize() * DisLaneWidth / 2);
                            split=split.Translation(-lane.Vec.Normalize() * mindistance);
                            if (ClosestPointInVertLines(split.P0, split, IniLanes.Select(e => e.Line)) < 10)
                                split.P0 = split.P0.Translation(Vector(split).Normalize() * DisLaneWidth / 2);
                            if (ClosestPointInVertLines(split.P1, split, IniLanes.Select(e => e.Line)) < 10)
                                split.P1 = split.P1.Translation(-Vector(split).Normalize() * DisLaneWidth / 2);
                            if (split.Length < minlength) continue;
                            Lane ln = new Lane(split, lane.Vec);
                            lanes.Add(ln);
                        }
                    }
                }
            }
            return lanes;
        }
        private bool CloseToWall(Coordinate point)
        {
            if (Walls.Count == 0) return false;
            var dis = ClosestPointInCurvesFast(point, Walls);
            if (dis < 10) return true;
            else return false;
        }
        private bool IsConnectedToLaneDouble(LineSegment line)
        {
            if (IsConnectedToLane(line, true) && IsConnectedToLane(line, false)) return true;
            else return false;
        }
        private int GenerateUsefulModules(LineSegment lane, Vector2D vec, List<Polygon> plys, ref int generatedcount)
        {
            int count = 0;
            var unittest = new LineSegment(lane);
            unittest = unittest.Translation(vec.Normalize() * MaxLength);
            var pltest = PolyFromPoints(new List<Coordinate>() { lane.P0, lane.P1, unittest.P1, unittest.P0 });
            var pltestsc = pltest.Clone();
            pltestsc=pltestsc.Scale(ScareFactorForCollisionCheck);
            var crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(pltestsc).Cast<Polygon>().ToList();
            crossed.AddRange(CarBoxesSpatialIndex.SelectCrossingGeometry(pltestsc).Cast<Polygon>());
            crossed.Add(Boundary);
            List<Coordinate> points = new List<Coordinate>();
            foreach (var o in crossed)
            {
                points.AddRange(o.Coordinates);
                //多线程：在单核模式中为pltest，调试出bug，临时改为pltestsc
                //points.AddRange(o.IntersectPoint(pltest));
                points.AddRange(o.IntersectPoint(pltestsc));
                var splitselves = new List<LineString>();
                try
                {
                    //points.AddRange(SplitCurve(o, pltestsc).Select(e => e.GetPointAtParameter(e.EndParam / 2)));
                    splitselves = SplitCurve(o, pltestsc).Where(f =>
                    {
                        if (f.Coordinates.Count()==2) return pltestsc.Contains(f.GetMidPoint());
                        else if (f.Coordinates.Count() > 2) return pltestsc.Contains(f.Envelope.Centroid);
                        else return false;
                    }).ToList();
                }
                catch
                {
                }
                foreach (var sp in splitselves)
                {
                    if (sp.Coordinates.Count() == 2) points.Add(sp.GetMidPoint());
                    else if (sp.Coordinates.Count() > 2)
                    {
                        points.AddRange(sp.GetEdges().Select(f => f.MidPoint));
                    }
                }
            }
            points = points.Where(e => pltest.IsPointInFast(e) || pltest.ClosestPoint(e).Distance(e) < 1).Distinct().ToList();
            LineSegment edgea = new LineSegment(lane.P0, unittest.P0);
            LineSegment edgeb = new LineSegment(lane.P1, unittest.P1);
            var pointsa = points.Where(e => edgea.ClosestPoint(e).Distance(e) <
                    DisVertCarLength + DisLaneWidth).OrderBy(p => edgea.ClosestPoint(p).Distance(lane.P0)).ToList();
            var pointsb = points.Where(e => edgeb.ClosestPoint(e).Distance(e) <
                      DisVertCarLength + DisLaneWidth).OrderBy(p => edgeb.ClosestPoint(p).Distance(lane.P1)).ToList();
            for (int i = 0; i < pointsa.Count - 1; i++)
            {
                if (edgea.ClosestPoint(pointsa[i]).Distance(pointsa[i]) < 1)
                {
                    pointsa.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < pointsb.Count - 1; i++)
            {
                if (edgeb.ClosestPoint(pointsb[i]).Distance(pointsb[i]) < 1)
                {
                    pointsb.RemoveAt(i);
                    i--;
                }
            }
            pointsa = pointsa.Select(e => edgea.ClosestPoint(e)).ToList();
            pointsb = pointsb.Select(e => edgeb.ClosestPoint(e)).ToList();
            Coordinate pta;
            Coordinate ptb;
            pointsa = pointsa.Where(e => e.Distance(lane.P0) > 1).OrderBy(e => e.Distance(lane.P0)).ToList();
            pointsb = pointsb.Where(e => e.Distance(lane.P1) > 1).OrderBy(e => e.Distance(lane.P1)).ToList();
            if (pointsa.ToArray().Length == 0) pta = lane.P0;
            else pta = pointsa.First();
            if (pointsb.ToArray().Length == 0) ptb = lane.P0;
            else ptb = pointsb.First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.ClosestPoint(pta).Distance(pta);
                if (disa < DisLaneWidth / 2)
                {
                    pta = pta.Translation((new Vector2D(pta, lane.P0)).Normalize() * (DisLaneWidth / 2 - disa));
                }
                var disb = la.Line.ClosestPoint(ptb).Distance(ptb);
                if (disb < DisLaneWidth / 2)
                {
                    ptb = ptb.Translation(new Vector2D(ptb, lane.P1).Normalize() * (DisLaneWidth / 2 - disb));
                }
            }
            LineSegment eb = new LineSegment(lane.P1, ptb);
            LineSegment ea = new LineSegment(lane.P0, pta);
            count += ((int)Math.Floor((ea.Length - DisLaneWidth / 2) / DisVertCarWidth));
            count += ((int)Math.Floor((eb.Length - DisLaneWidth / 2) / DisVertCarWidth));
            var pa = PolyFromPoints(new List<Coordinate>() { lane.P0, lane.P0.Translation(new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLane),
                pta.Translation(new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLane), pta });
            if (pa.Area > 0)
            {
                if (ClosestPointInVertLines(ea.P0, ea, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    Math.Abs(ClosestPointInVertLines(ea.P1, ea, IniLanes.Select(e => e.Line).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
                    ea.Length < DisLaneWidth / 2 + DisVertCarWidth * 5)
                {
                    count = 0;
                }
                else
                {
                    plys.Add(pa);
                    generatedcount++;
                }
            }
            var pb = PolyFromPoints(new List<Coordinate>() { lane.P1, lane.P1.Translation(-new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLane),
                 ptb.Translation(-new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLane),ptb});
            if (pb.Area > 0)
            {
                if (ClosestPointInVertLines(eb.P0, eb, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    Math.Abs(ClosestPointInVertLines(eb.P1, eb, IniLanes.Select(e => e.Line).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
    eb.Length < DisLaneWidth / 2 + DisVertCarWidth * 5)
                {
                    count = 0;
                }
                else
                {
                    plys.Add(pb);
                    generatedcount++;
                }
            }

            return count;
        }
        private PerpModlues ConstructPerpModules(Vector2D vec, List<LineSegment> ilanes)
        {
            PerpModlues result = new PerpModlues();
            int count = 0;
            int minindex = 0;
            int mincount = 9999;
            List<Polygon> plys = new List<Polygon>();
            vec = vec.Normalize() * MaxLength;
            for (int i = 0; i < ilanes.Count; i++)
            {
                int mintotalcount = 7;
                var unitbase = ilanes[i];
                int generatedcount = 0;
                var tmpplycount = plys.Count;
                var curcount = GenerateUsefulModules(unitbase, vec, plys, ref generatedcount);
                if (plys.Count > tmpplycount)
                {
                    var pl = plys[plys.Count - 1];
                    var lane = pl.GetEdges()[3];

                    if (ClosestPointInVertLines(lane.P0, lane, IniLanes.Select(e => e.Line)) <= DisLaneWidth / 2
                        && ClosestPointInVertLines(lane.P1, lane, IniLanes.Select(e => e.Line)) <= DisLaneWidth / 2)
                        mintotalcount = 16;
                }
                if (curcount < mintotalcount)
                {
                    ilanes.RemoveAt(i);
                    i--;
                    for (int j = 0; j < generatedcount; j++)
                    {
                        plys.RemoveAt(plys.Count - 1);
                    }
                    continue;
                }
                if (curcount < mincount)
                {
                    mincount = curcount;
                    minindex = i;
                }
                count += curcount;
            }
            result.Bounds = plys;
            result.Count = count;
            result.Lanes = ilanes;
            result.Mminindex = minindex;
            result.Vec = vec;
            return result;
        }
        private void GeneratePerpModuleBoxes(List<Lane> lanes)
        {
            SortLaneByDirection(lanes, LayoutMode);
            foreach (var lane in lanes)
            {
                var line = new LineSegment(lane.Line);
                List<LineSegment> ilanes = new List<LineSegment>();
                var segs = new List<LineSegment>();
                DivideCurveByLength(line, DisModulus, ref segs);
                ilanes.AddRange(segs.Where(t => Math.Abs(t.Length - DisModulus) < 1));
                int modulecount = ilanes.Count;
                int vertcount = ((int)Math.Floor((line.Length - modulecount * DisModulus) / DisVertCarWidth));
                PerpModlues perpModlue = ConstructPerpModules(lane.Vec, ilanes);
                for (int i = 0; i < vertcount; i++)
                {
                    var test = UpdataPerpModlues(perpModlue);
                    if (test.Count >= perpModlue.Count)
                    {
                        perpModlue = test;
                    }
                    else continue;
                }
                foreach (var pl in perpModlue.Bounds)
                {
                    var a = pl.GetEdges()[1];
                    var b = pl.GetEdges()[3];
                    var vec = new Vector2D(a.MidPoint, b.ClosestPoint(a.MidPoint, true));
                    IniLanes.Add(new Lane(a, vec.Normalize()));
                    CarModule module = new CarModule();
                    module.Box = pl;
                    module.Line = a;
                    module.Vec = vec;
                    CarModules.Add(module);
                }
                CarBoxes.AddRange(perpModlue.Bounds);
                CarBoxesSpatialIndex.Update(perpModlue.Bounds, new List<Polygon>());
            }
        }
        private PerpModlues UpdataPerpModlues(PerpModlues perpModlues)
        {
            PerpModlues result;
            int minindex = perpModlues.Mminindex;
            List<LineSegment> ilanes = new List<LineSegment>(perpModlues.Lanes);
            if (perpModlues.Lanes.Count == 0) return perpModlues;
            var vec_move = Vector(perpModlues.Lanes[0]).Normalize() * DisVertCarWidth;
            for (int i = 0; i < ilanes.Count; i++)
            {
                if (i >= minindex)
                {
                    ilanes[i]=ilanes[i].Translation(vec_move);
                }
            }
            result = ConstructPerpModules(perpModlues.Vec, ilanes);
            return result;
        }
        private static void UnifyLaneDirection(ref LineSegment lane, List<Lane> iniLanes)
        {
            var line = new LineSegment(lane);
            var lanes = iniLanes.Select(e => e.Line).Where(e => IsPerpLine(line, e)).ToList();
            var lanestrings = lanes.Select(e => e.ToLineString()).ToList();
            if (lanes.Count > 0)
            {
                if (ClosestPointInCurves(line.P1, lanes.Select(e =>e.ToLineString()).ToList()) < 1 && ClosestPointInCurves(line.P0, lanes.Select(e => e.ToLineString()).ToList()) < 1)
                {
                    if (line.P0.X - line.P1.X > 1000) line=new LineSegment(line.P1,line.P0);
                    else if (lanes.Count == 0 && line.P0.Y - line.P1.Y > 1000) line = new LineSegment(line.P1, line.P0);
                }
                else if (ClosestPointInCurves(line.P1, lanestrings) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.P0, lanestrings) > DisCarAndHalfLane + 1) line = new LineSegment(line.P1, line.P0);
                else if (ClosestPointInCurves(line.P1, lanestrings) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.P0, lanestrings) < DisCarAndHalfLane + 1
                    && ClosestPointInCurves(line.P1, lanestrings) < ClosestPointInCurves(line.P0, lanestrings)) line = new LineSegment(line.P1, line.P0);
            }
            else
            {
                if (line.P0.X - line.P1.X > 1000) line = new LineSegment(line.P1, line.P0);
                else if (lanes.Count == 0 && line.P0.Y - line.P1.Y > 1000) line = new LineSegment(line.P1, line.P0);
            }
            lane = line;
        }
        private void RemoveDuplicateCars()
        {
            for (int i = 1; i < CarSpots.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (CarSpots[i].Envelope.Centroid.Coordinate.Distance(CarSpots[j].Envelope.Centroid.Coordinate) < 1000)
                    {
                        CarSpots.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }
        private void RemoveCarsIntersectedWithBoundary()
        {
            var obspls = Obstacles.Where(e => e.Area > DisVertCarLength * DisLaneWidth * 5).ToList();
            var tmps = new List<Polygon>();
            foreach (var e in CarSpots)
            {
                var k = e.Clone();
                k=k.Scale( ScareFactorForCollisionCheck);
                var conda = Boundary.Contains(k.Envelope.Centroid.Coordinate);
                var condb = !IsInAnyPolys(k.Envelope.Centroid.Coordinate, obspls);
                //var condc = Boundary.Intersect(k, Intersect.OnBothOperands).Count == 0;
                //if (conda && condb && condc) tmps.Add(e);
                if (conda && condb)
                {
                    if (Boundary.ClosestPoint(k.Envelope.Centroid.Coordinate).Distance(k.Envelope.Centroid.Coordinate) > DisVertCarLength)
                        tmps.Add(e);
                    else if (Boundary.IntersectPoint(k).Count() == 0)
                        tmps.Add(e);
                }
            }
            CarSpots = tmps;
        }
        private void RemoveInvalidPillars()
        {
            List<Polygon> tmps = new List<Polygon>();
            foreach (var t in Pillars)
            {
                var clone = t.Clone();
                clone=clone.Scale( 0.5);
                if (ClosestPointInCurveInAllowDistance(clone.Envelope.Centroid.Coordinate, CarSpots, DisPillarLength + DisHalfCarToPillar))
                {
                    tmps.Add(t);
                }
            }
            Pillars = tmps;
        }
        private void ReDefinePillarDimensions()
        {
            if (HasImpactOnDepthForPillarConstruct)
            {
                Pillars = Pillars.Select(e =>
                {
                    var segobjs = e.GetEdges();
                    if (DisPillarLength < DisPillarDepth)
                    {
                        double t = DisPillarDepth;
                        DisPillarDepth = DisPillarLength;
                        DisPillarLength = t;
                        t = PillarNetLength;
                        PillarNetLength = PillarNetDepth;
                        PillarNetDepth = t;
                    }
                    var segs = segobjs.OrderByDescending(t => t.Length).ToList();
                    if (DisPillarLength < DisPillarDepth)
                        segs = segobjs.OrderBy(t => t.Length).ToList();
                    LineSegment a = new LineSegment();
                    LineSegment b = new LineSegment();
                    if (DisPillarLength != DisPillarDepth)
                    {
                        a = segs[0];
                        b = segs[1];
                    }
                    else
                    {
                        a = segs[0];
                        segs.RemoveAt(0);
                        b = segs.Where(t => IsParallelLine(t, a)).First();
                    }
                    b = new LineSegment(b.P1, b.P0);
                    a=a.Scale( PillarNetLength / a.Length);
                    b=b.Scale( PillarNetLength / b.Length);
                    a=a.Translation(new Vector2D(a.MidPoint, b.MidPoint).Normalize() * ThicknessOfPillarConstruct);
                    b=b.Translation(-new Vector2D(a.MidPoint, b.MidPoint).Normalize() * ThicknessOfPillarConstruct);
                    var pl = PolyFromLines(a, b);
                    return pl;
                }).ToList();
            }
        }
        public void GenerateCarsAndPillarsForEachLane(LineSegment line, Vector2D vec, double length_divided, double length_offset,
  bool add_to_car_spacialindex = true, bool judge_carmodulebox = true, bool adjust_pillar_edge = false, bool judge_modulebox = false,
  bool gfirstpillar = true, bool allow_pillar_in_wall = false, bool align_back_to_back = true, bool judge_in_obstacles = false, bool glastpillar = true, bool judge_intersect_bound = false,
  bool generate_middle_pillar = false, bool isin_backback = false)
        {
            int inipillar_count = Pillars.Count;
            //允许柱子穿墙
            if (allow_pillar_in_wall && GeneratePillars && Obstacles.Count > 0)
            {
                double dis_judge_under_building = 5000;
                if (ClosestPointInCurvesFast(line.P0, Obstacles.Select(e => new LineString(e.Coordinates)).ToList()) < dis_judge_under_building)
                {
                    var dis = ClosestPointInVertCurves(line.P0, line, IniLanes.Select(e => e.Line).ToList());
                    if (dis >= DisLaneWidth + DisPillarLength - 1 && Math.Abs(dis - DisCarAndHalfLane) > 1)
                        line = new LineSegment(line.P0.Translation(-Vector(line).Normalize() * DisPillarLength), line.P1);
                    else if (line.Length < DisVertCarWidth * 4)
                        line = new LineSegment(line.P0.Translation(-Vector(line).Normalize() * DisPillarLength), line.P1);
                }
            }
            if (line.Length == 0) return;
            //背靠背对齐
            if (Math.Abs(length_divided - DisVertCarWidth) < 1 && align_back_to_back)
            {
                double dis_judge_in_backtoback = 20000;
                var pts = line.P0.Translation(vec.Normalize() * DisVertCarLength * 1.5);
                var pte = pts.Translation(Vector(line).Normalize() * dis_judge_in_backtoback);
                var tl = new LineSegment(pts, pte);
                var tlbf = tl.Buffer(1);
                var crosscars = CarSpatialIndex.SelectCrossingGeometry(tlbf).Cast<Polygon>().OrderBy(t => t.Envelope.Centroid.Coordinate.Distance(pts)).ToList();
                if (crosscars.Count() > 1)
                {
                    for (int i = 1; i < crosscars.Count(); i++)
                    {
                        if (Math.Abs(crosscars[i].Envelope.Centroid.Coordinate.Distance(crosscars[i - 1].Envelope.Centroid.Coordinate) - DisVertCarWidth - DisPillarLength) < 10)
                        {
                            var p = crosscars[i].Coordinates.OrderBy(t => t.Distance(line.P0)).First();
                            var ponline_ex = line.ClosestPoint(p, true);
                            var dis = ponline_ex.Distance(line.P0) % (DisVertCarWidth * CountPillarDist + DisPillarLength);
                            line = new LineSegment(line.P0.Translation(Vector(line).Normalize() * (dis - DisPillarLength)), line.P1);
                            break;
                        }
                    }
                }
            }
            //长度划分
            var segobjs = new List<LineSegment>();
            LineSegment[] segs;
            if (GeneratePillars)
            {
                var dividecount = Math.Abs(length_divided - DisVertCarWidth) < 1 ? CountPillarDist : 1;
                //DivideCurveByDifferentLength(line, ref segobjs, DisPillarLength, 1, length_divided, dividecount);
                DivideCurveByKindsOfLength(line, ref segobjs, DisPillarLength, 1, DisHalfCarToPillar, 1,
                    length_divided, dividecount, DisHalfCarToPillar, 1);
            }
            else
            {
                DivideCurveByLength(line, length_divided, ref segobjs);
            }
            segs = segobjs.Where(e => Math.Abs(e.Length - length_divided) < 1).ToArray();
            Polygon precar = new Polygon(new LinearRing(new Coordinate[0]));
            int segscount = segs.Count();
            int c = 0;
            foreach (var seg in segs)
            {
                c++;
                var s = new LineSegment(seg);
                s=s.Translation(vec.Normalize() * (length_offset));
                var car = PolyFromPoints(new List<Coordinate>() { seg.P0, seg.P1, s.P1, s.P0 });
                var carsc = car.Clone();
                carsc=carsc.Scale(ScareFactorForCollisionCheck);
                var cond = ObstaclesSpatialIndex.SelectCrossingGeometry(carsc).Count == 0;
                if (judge_carmodulebox)
                {
                    cond = cond && (!IsInAnyPolys(carsc.Envelope.Centroid.Coordinate, CarBoxes))
                        && CarBoxesSpatialIndex.SelectCrossingGeometry(carsc).Count == 0;
                }
                else
                    cond = cond && CarSpatialIndex.SelectCrossingGeometry(carsc).Count == 0;
                if (cond)
                {
                    if (add_to_car_spacialindex)
                        AddToSpatialIndex(car, ref CarBoxesSpatialIndex);
                    AddToSpatialIndex(car, ref CarSpatialIndex);
                    CarSpots.Add(car);
                    if (Pillars.Count > 0)
                    {
                        if (car.Envelope.Contains(Pillars[Pillars.Count - 1].Envelope.Centroid))
                        {
                            Pillars.RemoveAt(Pillars.Count - 1);
                        }
                    }
                    //如果是生成该车道上的第一个车位，判断是否需要在前方生成柱子，如果需要则生成
                    if (precar.Area == 0)
                    {
                        if (gfirstpillar && GeneratePillars)
                        {
                            var ed = seg;
                            if (adjust_pillar_edge)
                            {
                                ed = s;
                                vec = -vec;
                            }
                            var pp = ed.P0.Translation(-Vector(ed).Normalize() * DisPillarLength);
                            var li = new LineSegment(pp, ed.P0);
                            var lf = new LineSegment(li);
                            lf=lf.Translation(vec.Normalize() * DisPillarDepth);
                            var pillar = PolyFromPoints(new List<Coordinate>() { li.P0, li.P1, lf.P1, lf.P0 });
                            pillar=pillar.Translation(-Vector(ed).Normalize() * DisHalfCarToPillar);
                            if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                            {
                                bool condg = true;
                                if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 2].IsPointInFast(pillar.Envelope.Centroid.Coordinate))
                                    condg = false;
                                if (condg)
                                {
                                    //AddToSpatialIndex(pillar, ref CarSpatialIndex);                           
                                    if (isin_backback)
                                        pillar=pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2));
                                    else
                                        pillar=pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2));
                                    Pillars.Add(pillar);
                                }
                            }
                            if (adjust_pillar_edge)
                            {
                                vec = -vec;
                            }
                        }
                        precar = car;
                    }
                    else
                    {
                        var dist = car.Envelope.Centroid.Coordinate.Distance(precar.Envelope.Centroid.Coordinate);
                        if (Math.Abs(dist - length_divided - DisPillarLength - DisHalfCarToPillar * 2) < 1 && GeneratePillars)
                        {
                            var ed = seg;
                            if (adjust_pillar_edge)
                            {
                                ed = s;
                                vec = -vec;
                            }
                            var pp = precar.ClosestPoint(ed.P0);
                            var li = new LineSegment(pp, ed.P0);
                            li.P1 = pp.Translation(Vector(li).Normalize() * DisPillarLength);
                            var lf = new LineSegment(li);
                            lf=lf.Translation(vec.Normalize() * DisPillarDepth);
                            var pillar = PolyFromPoints(new List<Coordinate>() { li.P0, li.P1, lf.P1, lf.P0 });
                            pillar=pillar.Translation(Vector(ed).Normalize() * DisHalfCarToPillar);
                            if (isin_backback)
                                pillar=pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2));
                            else
                                pillar=pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2));
                            if (Math.Abs(pillar.Area - DisPillarDepth * DisPillarLength) < 1)
                            {
                                if (add_to_car_spacialindex)
                                    AddToSpatialIndex(pillar, ref CarBoxesSpatialIndex);
                                Pillars.Add(pillar);
                                AddToSpatialIndex(pillar, ref CarSpatialIndex);
                            }
                            if (adjust_pillar_edge)
                            {
                                vec = -vec;
                            }
                        }
                        else { }
                        precar = car;
                    }
                    //判断是否需要生成最后一个柱子
                    if (glastpillar && c == segscount && GeneratePillars)
                    {
                        var ed = seg;
                        if (adjust_pillar_edge)
                        {
                            ed = s;
                            vec = -vec;
                        }
                        var pp = ed.P1.Translation(Vector(ed).Normalize() * DisPillarLength);
                        var li = new LineSegment(pp, ed.P1);
                        var lf = new LineSegment(li);
                        lf=lf.Translation(vec.Normalize() * DisPillarDepth);
                        var pillar = PolyFromPoints(new List<Coordinate>() { li.P0, li.P1, lf.P1, lf.P0 });
                        pillar=pillar.Translation(Vector(ed).Normalize() * DisHalfCarToPillar);
                        if (isin_backback)
                            pillar=pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2));
                        else
                            pillar=pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2));
                        if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                        {
                            bool condg = true;
                            if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 1].IsPointInFast(pillar.Envelope.Centroid.Coordinate))
                                condg = false;
                            if (condg)
                            {
                                Pillars.Add(pillar);
                            }
                        }
                        if (adjust_pillar_edge)
                        {
                            vec = -vec;
                        }
                    }
                }
            }
            //判断是否需要生成中柱
            if (generate_middle_pillar)
            {
                var middle_pillars = new List<Polygon>();
                for (int i = inipillar_count; i < Pillars.Count; i++)
                {
                    var p = Pillars[i].Clone();
                    double dist = DisVertCarLength - DisPillarMoveDeeplyBackBack;
                    p=p.Translation(vec.Normalize() * dist);
                    var pp = p.Clone();
                    pp = pp.Translation(vec.Normalize() * DisPillarDepth);
                    var pisegs = pp.GetEdges();
                    var piseg = pisegs.Where(e => IsPerpVector(Vector(e), vec)).First();
                    piseg = piseg.Scale((DisHalfCarToPillar * 2 + DisPillarLength) / DisPillarLength + 1);
                    var buffer = piseg.Buffer(1);
                    if (CarSpatialIndex.SelectCrossingGeometry(buffer).Count > 0)
                        middle_pillars.Add(p);
                }
                Pillars.AddRange(middle_pillars);
            }
        }
    }
}