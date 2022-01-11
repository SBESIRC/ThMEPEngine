using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public partial class ParkingPartitionBackup
    {

        private void SortModuleLaneByGeneratedLength(List<Lane> lanes, double distance, double minlengthsingle, double minlengthdouble)
        {
            var comparer = new LaneGModuleLengthComparer(this, distance, minlengthsingle, minlengthdouble);
            lanes.Sort(comparer);
        }
        private class LaneGModuleLengthComparer : IComparer<Lane>
        {
            public LaneGModuleLengthComparer(ParkingPartitionBackup partition, double distance, double minlengthsingle, double minlengthdouble)
            {
                Partition = partition;
                Distance = distance;
                Minlengthsingle = minlengthsingle;
                Minlengthdouble = minlengthdouble;
            }
            private ParkingPartitionBackup Partition = null;
            private double Distance;
            private double Minlengthsingle;
            private double Minlengthdouble;
            public int Compare(Lane a, Lane b)
            {
                var len_a = Partition.GetModuleLaneLengthGenerally(a, Distance, Minlengthsingle, Minlengthdouble);
                var len_b = Partition.GetModuleLaneLengthGenerally(b, Distance, Minlengthsingle, Minlengthdouble);
                if (len_a == len_b) return 0;
                else if (len_a > len_b) return -1;
                else return 1;
            }
        }

        private double GetModuleLaneLengthGenerally(Lane lane, double distance, double minlengthsingle, double minlengthdouble)
        {
            var line = lane.Line;
            var vec = lane.Vec;
            //不与车道线相连
            if (!lane.CanBeMoved || line.Length < minlengthsingle) return 0;
            if (ClosestPointInVertLines(line.StartPoint, line, IniLanes.Select(e => e.Line)) > 1
                && ClosestPointInVertLines(line.EndPoint, line, IniLanes.Select(e => e.Line)) > 1) return 0;
            var offsetline = CreateLine(line);
            double clinchdistance = IsEssentialToClinchObstacle(offsetline, vec);
            double length = 0;
            //贴近障碍物判断并返回
            if (clinchdistance > distance + DisLaneWidth / 2)
            {
                offsetline.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (clinchdistance - DisLaneWidth / 2)));
                var splited = SplitLine(offsetline, Boundary);
                var splitedcond = splited.Where(e => Boundary.Contains(e.GetCenter())).Where(e => e.Length > 1).ToArray();
                if (splitedcond.Length == 0)
                {
                    offsetline.Dispose();
                    splited.ForEach(e => e.Dispose());
                    return 0;
                }
                else
                {
                    foreach (var split in splitedcond)
                    {
                        length += split.Length;
                    }
                    offsetline.Dispose();
                    splited.ForEach(e => e.Dispose());
                    return length;
                }
            }
            offsetline.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (distance + DisLaneWidth / 2)));
            offsetline.TransformBy(Matrix3d.Scaling(10, offsetline.GetCenter()));
            var bdsplited = SplitLine(offsetline, Boundary);
            var bdsplitedcond = bdsplited.Where(e => Boundary.Contains(e.GetCenter()))
                .Where(e => ClosestPointInVertLines(e.StartPoint, e, IniLanes.Select(f => f.Line)) < 1
                    || ClosestPointInVertLines(e.EndPoint, e, IniLanes.Select(f => f.Line)) < 1)
                .Where(e => e.Length > 1).ToArray();
            if (bdsplitedcond.Length == 0)
            {
                //bdsplited.ForEach(e => e.Dispose());
                //offsetline.Dispose();
                return 0;
            }
            foreach (var split in bdsplitedcond)
            {
                //与车道模块相交判断
                var cbsplited = SplitLine(split, CarBoxes);
                var cbsplitedcond = cbsplited.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).ToArray();
                if (cbsplitedcond.Length == 0)
                {
                    //cbsplited.ForEach(e => e.Dispose());
                    continue;
                }
                var targetedline = cbsplitedcond.First();
                if (cbsplitedcond.Length > 1) targetedline = cbsplited.OrderBy(e => e.GetCenter().DistanceTo(line.GetClosestPointTo(e.GetCenter(), false))).First();
                var pl = CreatePolyFromPoints(new Point3d[] { line.GetClosestPointTo(targetedline.StartPoint,false),
                    line.GetClosestPointTo(targetedline.EndPoint,false),targetedline.EndPoint,targetedline.StartPoint});
                var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(pl).Cast<Polyline>();
                var points = new List<Point3d>();
                foreach (var c in crossed)
                {
                    points.AddRange(c.Vertices().Cast<Point3d>());
                    points.AddRange(c.Intersect(pl, Intersect.OnBothOperands));
                }
                points = points.Where(p => pl.Contains(p)).Select(p => targetedline.GetClosestPointTo(p, false)).Distinct().ToList();
                var obsplited = SplitLine(targetedline, points);
                foreach (var o in obsplited)
                {
                    var cond_a = ClosestPointInVertLines(o.StartPoint, o, IniLanes.Select(f => f.Line)) < 1
                        || ClosestPointInVertLines(o.EndPoint, o, IniLanes.Select(f => f.Line)) < 1;
                    var cond_b = o.Length > minlengthsingle;
                    if (cond_a && cond_b)
                        length += o.Length;
                }
                //cbsplited.ForEach(p => p.Dispose());
                //pl.Dispose();
                //obsplited.ForEach(e => e.Dispose());
            }
            //offsetline.Dispose();
            return length;
        }

        private bool GenerateModuLeLaneGenerally(Lane lane, int index, double distance, double minlengthsingle, double minlengthdouble)
        {
            var generated = false;
            var line = lane.Line;
            var vec = lane.Vec;
            //不与车道线相连
            if (!lane.CanBeMoved || line.Length < minlengthsingle) return generated;
            if (ClosestPointInVertLines(line.StartPoint, line, IniLanes.Select(e => e.Line)) > 1
                && ClosestPointInVertLines(line.EndPoint, line, IniLanes.Select(e => e.Line)) > 1) return generated;
            var offsetline = CreateLine(line);
            double clinchdistance = IsEssentialToClinchObstacle(offsetline, vec);
            if (clinchdistance > distance + DisLaneWidth / 2)
            {
                bool setmove = false;
                offsetline.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (clinchdistance - DisLaneWidth / 2)));
                var clinchbdsplited = SplitLine(offsetline, Boundary);
                var clinchbdsplitedcond = clinchbdsplited.Where(e => Boundary.Contains(e.GetCenter()))
                    .Where(e => ClosestPointInVertLines(e.StartPoint, e, IniLanes.Select(f => f.Line)) < 1
                    || ClosestPointInVertLines(e.EndPoint, e, IniLanes.Select(f => f.Line)) < 1)
                    .Where(e => e.Length > 1).ToArray();
                foreach (var split in clinchbdsplitedcond)
                {
                    var cbsplited = SplitLine(split, CarBoxes);
                    var cbsplitedcond = cbsplited.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).ToArray();
                    if (cbsplitedcond.Length == 0)
                    {
                        //cbsplited.ForEach(e => e.Dispose());
                        continue;
                    }
                    var targetedline = cbsplited.First();
                    if (cbsplitedcond.Length > 1)
                    {
                        cbsplited = cbsplited.OrderBy(e => e.GetCenter().DistanceTo(line.GetClosestPointTo(e.GetCenter(), false))).ToArray();
                        targetedline = cbsplited.First();
                        //for (int i = 1; i < cbsplited.Length; i++) cbsplited[i].Dispose();
                    }
                    if (!setmove)
                    {
                        IniLanes[index].CanBeMoved = false;
                        setmove = true;
                    }
                    Lane ln = new Lane(targetedline, -vec);
                    generated = true;
                    IniLanes.Add(ln);
                }
                //clinchbdsplited.ForEach(e => e.Dispose());
                if (generated) return generated;
            }
            offsetline.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (distance /*+ DisLaneWidth / 2*/)));
            offsetline.TransformBy(Matrix3d.Scaling(10, offsetline.GetCenter()));
            var bdsplited = SplitLine(offsetline, Boundary);
            var bdsplitedcond = bdsplited.Where(e => Boundary.Contains(e.GetCenter()))
                .Where(e => ClosestPointInVertLines(e.StartPoint, e, IniLanes.Select(f => f.Line)) < 1
                    || ClosestPointInVertLines(e.EndPoint, e, IniLanes.Select(f => f.Line)) < 1)
                .Where(e => e.Length > 1).ToArray();
            var generated_plys = new List<Polyline>();
            foreach (var splits in bdsplitedcond)
            {
                splits.TransformBy(Matrix3d.Displacement(-vec.GetNormal() * (distance / 2 /*+ DisLaneWidth / 2*/)));
                var cbspliteds = SplitLine(splits, CarBoxes);
                var cbsplitedconds = cbspliteds.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).ToArray();
                if (cbsplitedconds.Length == 0) continue;
                foreach (var split in cbsplitedconds)
                {
                    split.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (distance / 2 + DisLaneWidth / 2)));
                    var cbsplited = SplitLine(split, CarBoxes);
                    var cbsplitedcond = cbsplited.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).Where(e => e.Length > 2400).ToArray();
                    if (cbsplitedcond.Length == 0) continue;
                    foreach (var targetedline in cbsplitedcond)
                    {                      
                        var pl = CreatePolyFromPoints(new Point3d[] { line.GetClosestPointTo(targetedline.StartPoint,false),
                    line.GetClosestPointTo(targetedline.EndPoint,false),targetedline.EndPoint,targetedline.StartPoint});
                        var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(pl).Cast<Polyline>();
                        var points = new List<Point3d>();
                        foreach (var c in crossed)
                        {
                            points.AddRange(c.Vertices().Cast<Point3d>());
                            points.AddRange(c.Intersect(pl, Intersect.OnBothOperands));
                        }
                        points = points.Where(p => pl.Contains(p)).Select(p => targetedline.GetClosestPointTo(p, false)).Distinct().ToList();
                        var obsplited = SplitLine(targetedline, points);
                        bool setmove = false;

                        foreach (var o in obsplited)
                        {
                            var ini = SplitLine(o, IniLanes.Select(t => t.Line).ToList()).Where(t => t.Length > minlengthsingle);
                            foreach (var r in ini)
                            {
                                r.TransformBy(Matrix3d.Displacement(-vec.GetNormal() * DisLaneWidth / 2));
                                var cond_a = ClosestPointInVertLines(r.StartPoint, r, IniLanes.Select(f => f.Line)) < 1
                             || ClosestPointInVertLines(r.EndPoint, r, IniLanes.Select(f => f.Line)) < 1;
                                var cond_b = r.Length > minlengthsingle;
                                var cond_c = !HasParallelLaneExistAlready(r, vec, DisPreventGenerateModuleLane);
                                var cond_d = true;
                                if (cond_a && cond_b && cond_c)
                                {
                                    if (!setmove)
                                    {
                                        IniLanes[index].CanBeMoved = false;
                                        setmove = true;
                                    }
                                    Lane ln = new Lane(r, vec);
                                    IniLanes.Add(ln);
                                    var plg = CreatePolyFromPoints(new Point3d[] { r.StartPoint,r.EndPoint,
                        r.EndPoint.TransformBy(Matrix3d.Displacement(-vec.GetNormal()*DisCarAndHalfLane)),
                        r.StartPoint.TransformBy(Matrix3d.Displacement(-vec.GetNormal()*DisCarAndHalfLane))});
                                    var pt_on_pl = plg.GetClosestPointTo(line.StartPoint, false);
                                    var pt_on_line = line.GetClosestPointTo(pt_on_pl, false);
                                    if(Math.Abs(pt_on_pl.DistanceTo(pt_on_line)-DisCarAndHalfLane)>1) cond_d=false;
                                    if (!IsInAnyBoxes(plg.GetRecCentroid(), CarBoxes) && cond_d)
                                    {
                                        plg.Scale(plg.GetRecCentroid(), ScareFactorForCollisionCheck);
                                        if (ObstaclesSpatialIndex.SelectCrossingPolygon(plg).Count == 0
                                            && Boundary.Intersect(plg, Intersect.OnBothOperands).Count == 0
                                            && Boundary.Contains(pl.GetRecCentroid()))
                                        {
                                            CarBoxes.Add(plg);
                                            generated_plys.Add(plg);
                                            generated = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                   
                    //cbsplited.ForEach(p => p.Dispose());
                    // pl.Dispose();
                    //obsplited.ForEach(e => e.Dispose());
                }
            }
            if (generated)
            {
                var cbspliteds = SplitLine(line, CarBoxes);
                var cbsplitedconds = cbspliteds.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).ToArray();
                if (cbsplitedconds.Length == 0) return generated;
                foreach (var split in cbsplitedconds)
                {
                    split.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (distance / 2)));
                    var cbsplited = SplitLine(split, CarBoxes);
                    var cbsplitedcond = cbsplited.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).Where(e => e.Length > 2400).ToArray();
                    if (cbsplitedcond.Length == 0) continue;
                    foreach (var targetedline in cbsplitedcond)
                    {
                        var pl = CreatePolyFromPoints(new Point3d[] { line.GetClosestPointTo(targetedline.StartPoint,false),
                    line.GetClosestPointTo(targetedline.EndPoint,false),targetedline.EndPoint,targetedline.StartPoint});
                        var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(pl).Cast<Polyline>();
                        var points = new List<Point3d>();
                        foreach (var c in crossed)
                        {
                            points.AddRange(c.Vertices().Cast<Point3d>());
                            points.AddRange(c.Intersect(pl, Intersect.OnBothOperands));
                        }
                        points = points.Where(p => pl.Contains(p)).Select(p => targetedline.GetClosestPointTo(p, false)).Distinct().ToList();
                        var obsplited = SplitLine(targetedline, points);
                        foreach (var obs in obsplited)
                        {
                            var ini = SplitLine(obs, IniLanes.Select(t => t.Line).ToList()).Where(t => t.Length > minlengthsingle);
                            foreach (var f in ini)
                            {
                                var cond_b = f.Length > minlengthsingle;
                                var cond_c = false;
                                foreach (var ply in generated_plys)
                                {
                                    var pt_on_ply = ply.GetClosestPointTo(f.GetCenter(), false);
                                    if (f.GetClosestPointTo(pt_on_ply, false).DistanceTo(pt_on_ply) < 2000)
                                    {
                                        cond_c = true;
                                        break;
                                    }
                                }
                                if (cond_b && cond_c )
                                {
                                    var plg = CreatePolyFromPoints(new Point3d[] { f.StartPoint,f.EndPoint,
                        f.EndPoint.TransformBy(Matrix3d.Displacement(-vec.GetNormal()*DisCarAndHalfLane)),
                        f.StartPoint.TransformBy(Matrix3d.Displacement(-vec.GetNormal()*DisCarAndHalfLane))});
                                    if (!IsInAnyBoxes(plg.GetRecCentroid(), CarBoxes))
                                    {
                                        plg.Scale(plg.GetRecCentroid(), ScareFactorForCollisionCheck);
                                        if (ObstaclesSpatialIndex.SelectCrossingPolygon(plg).Count == 0
                                            && Boundary.Intersect(plg, Intersect.OnBothOperands).Count == 0
                                            && Boundary.Contains(plg.GetRecCentroid()))
                                            CarBoxes.Add(plg);
                                    }

                                }
                            }
                        }
                    }
                 
                    //var cbsplited = SplitLine(line, CarBoxes);
                    //var cbsplitedcond = cbsplited.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).ToArray();
                    //if (cbsplitedcond.Length == 0) return generated;
                    //var targetedline = cbsplitedcond.First();
                    //if (cbsplitedcond.Length > 1) targetedline = cbsplited.OrderBy(e => e.GetCenter().DistanceTo(line.GetClosestPointTo(e.GetCenter(), false))).First();
                    //var pl = CreatePolyFromPoints(new Point3d[] { targetedline.StartPoint,targetedline.EndPoint,
                    //    targetedline.EndPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal()*DisCarAndHalfLane)),
                    //    targetedline.StartPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal()*DisCarAndHalfLane))});
                    //var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(pl).Cast<Polyline>();
                    //var points = new List<Point3d>();
                    //foreach (var c in crossed)
                    //{
                    //    points.AddRange(c.Vertices().Cast<Point3d>());
                    //    points.AddRange(c.Intersect(pl, Intersect.OnBothOperands));
                    //}
                    //points = points.Where(p => pl.Contains(p)).Select(p => targetedline.GetClosestPointTo(p, false)).Distinct().ToList();
                    //var obsplited = SplitLine(targetedline, points);
                    //foreach (var o in obsplited)
                    //{
                    //    var cond_b = o.Length > minlengthsingle;
                    //    o.TransformBy(Matrix3d.Displacement(-vec.GetNormal() * DisLaneWidth / 2));
                    //    if (cond_b)
                    //    {
                    //        var plg = CreatePolyFromPoints(new Point3d[] { o.StartPoint,o.EndPoint,
                    //        o.EndPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal()*DisCarAndHalfLane)),
                    //        o.StartPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal()*DisCarAndHalfLane))});
                    //        CarBoxes.Add(plg);
                    //    }
                    //}
                }
            }
            return generated;
        }

        private bool HasParallelLaneExistAlready(Line line, Vector3d vec, double dist)
        {
            var lanes = IniLanes.Where(e => IsParallelLine(line, e.Line)).Select(e => e.Line);
            var l = CreateLine(line);
            l.TransformBy(Matrix3d.Displacement(vec.GetNormal() * dist));
            var pl = CreatePolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, l.EndPoint, l.StartPoint });
            lanes = lanes.Where(e => e.Intersect(pl, Intersect.OnBothOperands).Count > 0);
            if (lanes.Count() == 0) return false;
            foreach (var lane in lanes)
            {
                if (lane.GetClosestPointTo(line.StartPoint, false).DistanceTo(lane.GetClosestPointTo(line.EndPoint, false)) < line.Length / 2)
                {
                    if (lane.GetClosestPointTo(line.GetCenter(), true).DistanceTo(line.GetCenter()) > DisLaneWidth)
                        return true;
                }
            }
            return false;
        }

        private double IsEssentialToClinchObstacle(Line line, Vector3d vec)
        {
            if (IsPerpVector(vec, Vector3d.YAxis)) return -1;
            if (vec.Y < 0) return -1;
            double distance = 10e9;
            var l = CreateLine(line);
            l.TransformBy(Matrix3d.Displacement(vec * MaxLength));
            var pl = CreatePolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, l.EndPoint, l.StartPoint });
            var points = ObstacleVertexes.Where(e => pl.GeometricExtents.IsPointIn(e));
            foreach (var p in points)
            {
                var dis = line.GetClosestPointTo(p, false).DistanceTo(p);
                if (dis < distance) distance = dis;
            }
            if (distance == 10e9) return -1;
            var ul = CreateLine(line);
            ul.TransformBy(Matrix3d.Displacement(vec.GetNormal() * distance));
            var lt = new Line(line.GetCenter().TransformBy(Matrix3d.Displacement(vec.GetNormal())), ul.GetCenter());
            bool found = false;
            foreach (var lane in IniLanes.Select(e => e.Line))
            {
                if (lt.Intersect(lane, Intersect.OnBothOperands).Count > 0)
                {
                    found = true;
                    break;
                }
            }
            l.Dispose();
            pl.Dispose();
            ul.Dispose();
            lt.Dispose();
            if (found) return -1;
            return distance;
        }

        private bool GenerateAdjLanesFunc(Lane lane, bool isStartpt)
        {
            var generate_adj_lanes = false;
            Point3d pt;
            Point3d ps;
            if (isStartpt)
            {
                pt = lane.Line.StartPoint;
                ps = pt.TransformBy(Matrix3d.Displacement(CreateVector(lane.Line).GetNormal() * DisCarAndHalfLane));
            }
            else
            {
                pt = lane.Line.EndPoint;
                ps = pt.TransformBy(Matrix3d.Displacement(-CreateVector(lane.Line).GetNormal() * DisCarAndHalfLane));
            }
            var lt = CreateLineFromStartPtAndVector(ps, lane.Vec, MaxLength);
            Vector3d vec = CreateVector(lane.Line).GetNormal();
            var ptest = ps.TransformBy(Matrix3d.Displacement(vec));
            if (ptest.DistanceTo(lane.Line.StartPoint) < DisCarAndHalfLane || ptest.DistanceTo(lane.Line.EndPoint) < DisCarAndHalfLane)
            {
                vec = -vec;
            }
            lt = SplitLine(lt, CarBoxes).Where(e => e.Length > 1).First();
            if(IsInAnyBoxes(lt.GetCenter(),CarBoxes)) return generate_adj_lanes;
            lt = SplitLine(lt, Boundary).Where(e => e.GetLength() > 1).Cast<Line>().First();
            double clinchdistance = IsEssentialToClinchObstacle(lt, vec);
            if (clinchdistance > DisCarAndHalfLane + DisLaneWidth / 2)
            {
                lt.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (clinchdistance - DisLaneWidth / 2)));
                var clinchbdsplited = SplitLine(lt, Boundary);
                var clinchbdsplitedcond = clinchbdsplited.Where(e => Boundary.Contains(e.GetCenter()))
                    .Where(e => ClosestPointInVertLines(e.StartPoint, e, IniLanes.Select(f => f.Line)) < 1
                    || ClosestPointInVertLines(e.EndPoint, e, IniLanes.Select(f => f.Line)) < 1)
                    .Where(e => e.Length > 1).ToArray();
                foreach (var split in clinchbdsplitedcond)
                {
                    var cbsplited = SplitLine(split, CarBoxes);
                    var cbsplitedcond = cbsplited.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes)).ToArray();
                    if (cbsplitedcond.Length == 0)
                    {
                        //cbsplited.ForEach(e => e.Dispose());
                        continue;
                    }
                    var targetedline = cbsplited.First();
                    if (cbsplitedcond.Length > 1)
                    {
                        cbsplited = cbsplited.OrderBy(e => e.GetCenter().DistanceTo(lane.Line.GetClosestPointTo(e.GetCenter(), false))).ToArray();
                        targetedline = cbsplited.First();
                        //for (int i = 1; i < cbsplited.Length; i++) cbsplited[i].Dispose();
                    }
                    Lane ln = new Lane(targetedline, -vec);
                    generate_adj_lanes = true;
                    IniLanes.Add(ln);
                }
                //clinchbdsplited.ForEach(e => e.Dispose());
                if (generate_adj_lanes) return generate_adj_lanes;
            }
            var buffer = lt.Buffer(DisLaneWidth / 2);
            var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(buffer).Cast<Polyline>().ToList();
            crossed.AddRange(CarBoxes.Where(e => e.Intersect(buffer, Intersect.OnBothOperands).Count > 0));
            var points = new List<Point3d>();
            foreach (var c in crossed)
            {
                points.AddRange(c.Vertices().Cast<Point3d>());
                points.AddRange(c.Intersect(buffer, Intersect.OnBothOperands));
                points.AddRange(c.Intersect(lt, Intersect.OnBothOperands));
            }
            points = points.Where(p => buffer.Contains(p) || buffer.GetClosePoint(p).DistanceTo(p) < 1).Select(p => lt.GetClosestPointTo(p, false)).Distinct().ToList();
            var tarline = SplitLine(lt, points).Where(e => e.Length > 1).First();
            if (IsInAnyBoxes(tarline.GetCenter(), CarBoxes)) return generate_adj_lanes;
            if (tarline.Length < LengthCanGAdjLaneConnectSingle) return generate_adj_lanes;
            if (HasParallelLaneExistAlready(tarline, vec, DisPreventGenerateAdjLane)) return generate_adj_lanes;
            Lane le = new Lane(tarline, vec);
            IniLanes[IniLanes.Count - 1].GeneratedAdjLane = true;
            IniLanes.Add(le);
            var pl = CreatePolyFromPoints(new Point3d[] { tarline.StartPoint,tarline.EndPoint,
            tarline.EndPoint.TransformBy(Matrix3d.Displacement(-vec.GetNormal()*DisCarAndHalfLane)),
            tarline.StartPoint.TransformBy(Matrix3d.Displacement(-vec.GetNormal()*DisCarAndHalfLane))});
            pl.Scale(pl.GetRecCentroid(), ScareFactorForCollisionCheck);
            if (ObstaclesSpatialIndex.SelectCrossingPolygon(pl).Count == 0
                && Boundary.Intersect(pl, Intersect.OnBothOperands).Count == 0
                && Boundary.Contains(pl.GetRecCentroid()))
            {
                CarBoxes.Add(pl);
                generate_adj_lanes = true;
            }
            return generate_adj_lanes;
        }


    }
}
