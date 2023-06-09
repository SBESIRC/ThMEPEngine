﻿using Autodesk.AutoCAD.DatabaseServices;
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
    public partial class ParkingPartition : IEquatable<ParkingPartition>
    {
        /// <summary>
        /// Construct the class pmmodule.
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="ilanes"></param>
        /// <param name="boundobstacles"></param>
        /// <returns></returns>
        private PMModlues ConstructPMModules(Vector3d vec, List<Line> ilanes, List<Polyline> boundobstacles)
        {
            PMModlues result = new PMModlues();
            int count = 0;
            int minindex = 0;
            int mincount = 9999;
            List<Polyline> plys = new List<Polyline>();
            vec = vec.GetNormal() * MaxDistance;         
            for (int i = 0; i < ilanes.Count; i++)
            {
                int mintotalcount = MinCountAllowGVertCarOnLine == 3 ? 6 : 7;
                var unitbase = ilanes[i];
                int generatedcount = 0;
                var tmpplycount = plys.Count;
                var curcount = GenerateUsefulModules(unitbase, vec, boundobstacles, plys, ref generatedcount);
                if (plys.Count > tmpplycount)
                {
                    var pl = plys[plys.Count - 1];
                    var segs = new DBObjectCollection();
                    pl.Explode(segs);
                    var lane = (Line)segs[3];

                    if (ClosestPointInVertLines(lane.StartPoint, lane, IniLaneLines) <= DisLaneWidth / 2
                        && ClosestPointInVertLines(lane.EndPoint, lane, IniLaneLines) <= DisLaneWidth / 2)
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
            //PModuleBox.AddRange(plys);
            result.Bounds = plys;
            result.Count = count;
            result.Lanes = ilanes;
            result.Mminindex = minindex;
            result.Vec = vec;
            return result;
        }

        /// <summary>
        /// Undate the class pmmodudle.
        /// </summary>
        /// <param name="pMModlues"></param>
        /// <param name="boundobstacles"></param>
        /// <returns></returns>
        private PMModlues UpdataPMModlues(PMModlues pMModlues, List<Polyline> boundobstacles)
        {
            PMModlues result;
            int minindex = pMModlues.Mminindex;
            List<Line> ilanes = new List<Line>(pMModlues.Lanes);
            if (pMModlues.Lanes.Count == 0) return pMModlues;
            var vec_move = CreateVector(pMModlues.Lanes[0]).GetNormal() * DisVertCarWidth;
            for (int i = 0; i < ilanes.Count; i++)
            {
                if (i >= minindex)
                {
                    ilanes[i].TransformBy(Matrix3d.Displacement(vec_move));
                }
            }
            result = ConstructPMModules(pMModlues.Vec, ilanes, boundobstacles);
            return result;
        }

        /// <summary>
        /// Generate pmmodules in the smallest partition unit and return the total count of car spots in the pmmodules.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="vec"></param>
        /// <param name="boundobstacles"></param>
        /// <param name="plys"></param>
        /// <returns></returns>
        private int GenerateUsefulModules(Line lane, Vector3d vec, List<Polyline> boundobstacles, List<Polyline> plys, ref int generatedcount)
        {
            int count = 0;
            Line unittest = CreateLine(lane);
            unittest.TransformBy(Matrix3d.Displacement(vec.GetNormal() * MaxDistance));
            var pltest = CreatePolyFromPoints(new Point3d[] { lane.StartPoint, lane.EndPoint, unittest.EndPoint, unittest.StartPoint });
            ThCADCoreNTSSpatialIndex sindexwalls = new ThCADCoreNTSSpatialIndex(boundobstacles.ToCollection());
            //sindexwalls.Update(PModuleBox.ToCollection(), new DBObjectCollection());
            var pltestsc = pltest.Clone() as Polyline;
            pltestsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pltestsc.GetRecCentroid()));
            var crossed = sindexwalls.SelectCrossingPolygon(pltestsc).Cast<Polyline>().ToList();
            crossed.AddRange(ObstaclesSpatialIndex.SelectCrossingPolygon(pltestsc).Cast<Polyline>());
            crossed.AddRange(CarSpatialIndex.SelectCrossingPolygon(pltestsc).Cast<Polyline>());
            List<Point3d> points = new List<Point3d>();
            foreach (var o in crossed)
            {
                points.AddRange(o.Vertices().Cast<Point3d>().ToArray());
                points.AddRange(o.Intersect(pltest, Intersect.OnBothOperands));
            }
            points = points.Where(e => pltest.IsPointInFast(e) || pltest.GetClosePoint(e).DistanceTo(e) < 1).Distinct().ToList();
            Line edgea = new Line(lane.StartPoint, unittest.StartPoint);
            Line edgeb = new Line(lane.EndPoint, unittest.EndPoint);
            var pointsa = points.Where(e => edgea.GetClosestPointTo(e, false).DistanceTo(e) <
                    DisVertCarLength + DisLaneWidth).OrderBy(p => edgea.GetClosestPointTo(p, false).DistanceTo(lane.StartPoint)).ToList();
            var pointsb = points.Where(e => edgeb.GetClosestPointTo(e, false).DistanceTo(e) <
                      DisVertCarLength + DisLaneWidth).OrderBy(p => edgeb.GetClosestPointTo(p, false).DistanceTo(lane.EndPoint)).ToList();
            for (int i = 0; i < pointsa.Count - 1; i++)
            {
                if (edgea.GetClosestPointTo(pointsa[i], false).DistanceTo(pointsa[i]) < 1)
                {
                    pointsa.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < pointsb.Count - 1; i++)
            {
                if (edgeb.GetClosestPointTo(pointsb[i], false).DistanceTo(pointsb[i]) < 1)
                {
                    pointsb.RemoveAt(i);
                    i--;
                }
            }
            pointsa = pointsa.Select(e => edgea.GetClosestPointTo(e, false)).ToList();
            pointsb = pointsb.Select(e => edgeb.GetClosestPointTo(e, false)).ToList();
            Point3d pta;
            Point3d ptb;
            if (pointsa.ToArray().Length == 0) pta = lane.StartPoint;
            else pta = pointsa.Where(e => e.DistanceTo(lane.StartPoint) > 1).OrderBy(e => e.DistanceTo(lane.StartPoint)).First();
            if (pointsb.ToArray().Length == 0) ptb = lane.StartPoint;
            else ptb = pointsb.Where(e => e.DistanceTo(lane.EndPoint) > 1).OrderBy(e => e.DistanceTo(lane.EndPoint)).First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.GetClosestPointTo(pta, false).DistanceTo(pta);
                if (disa < DisLaneWidth / 2)
                {
                    pta = pta.TransformBy(Matrix3d.Displacement(CreateVector(pta, lane.StartPoint).GetNormal() * (DisLaneWidth / 2 - disa)));
                }
                var disb = la.Line.GetClosestPointTo(ptb, false).DistanceTo(ptb);
                if (disb < DisLaneWidth / 2)
                {
                    ptb = ptb.TransformBy(Matrix3d.Displacement(CreateVector(ptb, lane.EndPoint).GetNormal() * (DisLaneWidth / 2 - disb)));
                }
            }
            Line eb = new Line(lane.EndPoint, ptb);
            Line ea = new Line(lane.StartPoint, pta);
            count += ((int)Math.Floor((ea.Length - DisLaneWidth / 2) / DisVertCarWidth));
            count += ((int)Math.Floor((eb.Length - DisLaneWidth / 2) / DisVertCarWidth));
            var pa = CreatePolyFromPoints(new Point3d[] { lane.StartPoint, lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),
                pta.TransformBy(Matrix3d.Displacement(CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)), pta });
            if (pa.Area > 0)
            {
                if (ClosestPointInVertCurves(ea.StartPoint, ea, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    Math.Abs(ClosestPointInVertCurves(ea.EndPoint, ea, IniLanes.Select(e => e.Line).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
                    ea.Length < DisLaneWidth / 2 + DisVertCarWidth * 6)
                {
                    pa.Dispose();
                }
                else
                {
                    plys.Add(pa);
                    generatedcount++;
                }
            }
            var pb = CreatePolyFromPoints(new Point3d[] { lane.EndPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),
                 ptb.TransformBy(Matrix3d.Displacement(-CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),ptb});
            if (pb.Area > 0)
            {
                if (ClosestPointInVertCurves(eb.StartPoint, eb, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    Math.Abs(ClosestPointInVertCurves(eb.EndPoint, eb, IniLanes.Select(e => e.Line).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
    eb.Length < DisLaneWidth / 2 + DisVertCarWidth * 6)
                {
                    pb.Dispose();
                }
                else
                {
                    plys.Add(pb);
                    generatedcount++;
                }
            }
            unittest.Dispose();
            pltest.Dispose();
            pltestsc.Dispose();
            ea.Dispose();
            eb.Dispose();
            return count;
        }

        private void GenerateCarsAndPillarsForEachLane(Line line, Vector3d vec, double length_divided, double length_offset, bool judge_lanebox = true,
            bool judge_carmodulebox = true, bool reverse_dividesequence = false, bool adjust_pillar_edge = false, bool judge_modulebox = false,
            bool gfirstpillar = false, bool allow_pillar_in_wall = false, bool judge_in_obstacles = false, bool glastpillar = true, List<Polyline> crs = null)
        {
            if (allow_pillar_in_wall && GeneratePillars)
            {
                var dis = ClosestPointInVertCurves(line.StartPoint, line, IniLanes.Select(e => e.Line).ToList());
                if (dis >= DisLaneWidth + DisPillarLength - 1 && Math.Abs(dis - DisCarAndHalfLane) > 1)
                    line = new Line(line.StartPoint.TransformBy(Matrix3d.Displacement(-CreateVector(line).GetNormal() * DisPillarLength)), line.EndPoint);
                else if (line.Length < DisVertCarWidth * 4)
                    line = new Line(line.StartPoint.TransformBy(Matrix3d.Displacement(-CreateVector(line).GetNormal() * DisPillarLength)), line.EndPoint);
            }

            if (Math.Abs(length_divided - DisVertCarWidth) < 1)
            {
                var pts = line.StartPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisVertCarLength * 1.5));
                var pte = pts.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * 50000));
                var tl = new Line(pts, pte);
                var tlbf = tl.Buffer(1);
                var crosscars = CarSpatialIndex.SelectCrossingPolygon(tlbf).Cast<Polyline>().OrderBy(t => t.GetRecCentroid().DistanceTo(pts)).ToList();
                if (crosscars.Count > 1)
                {
                    for (int i = 1; i < crosscars.Count; i++)
                    {
                        if (Math.Abs(crosscars[i].GetRecCentroid().DistanceTo(crosscars[i - 1].GetRecCentroid()) - DisVertCarWidth - DisPillarLength) < 10)
                        {
                            var p = crosscars[i].Vertices().Cast<Point3d>().OrderBy(t => t.DistanceTo(line.StartPoint)).First();
                            var ponline_ex = line.GetClosestPointTo(p, true);
                            var dis = ponline_ex.DistanceTo(line.StartPoint) % (DisVertCarWidth * CountPillarDist + DisPillarLength);
                            line = new Line(line.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * (dis - DisPillarLength))), line.EndPoint);
                            break;
                        }
                    }
                }
            }

            var segobjs = new DBObjectCollection();
            Line[] segs;
            if (GeneratePillars)
            {
                var dividecount = Math.Abs(length_divided - DisVertCarWidth) < 1 ? CountPillarDist : 1;
                if (reverse_dividesequence)
                {
                    DivideCurveByDifferentLength(line, ref segobjs, DisPillarLength, 1, length_divided, dividecount);
                }
                else
                {
                    DivideCurveByDifferentLength(line, ref segobjs, length_divided, dividecount, DisPillarLength, 1);
                }
            }
            else
            {
                DivideCurveByLength(line, length_divided, ref segobjs);
            }
            segs = segobjs.Cast<Line>().Where(e => Math.Abs(e.Length - length_divided) < 1).ToArray();
            var precar = new Polyline();
            int segscount = segs.Count();
            int c = 0;
            foreach (var seg in segs)
            {
                c++;
                var s = CreateLine(seg);
                s.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (length_offset)));
                var car = CreatePolyFromPoints(new Point3d[] { seg.StartPoint, seg.EndPoint, s.EndPoint, s.StartPoint });
                var carsc = car.Clone() as Polyline;
                carsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, carsc.GetRecCentroid()));
                if (judge_lanebox)
                {
                    bool condbox = false;
                    foreach (var box in Laneboxes)
                    {
                        if (box.GetClosestPointTo(carsc.GetRecCentroid(), false).DistanceTo(carsc.GetRecCentroid()) < DisVertCarLength)
                        {
                            if (carsc.Intersect(box, Intersect.OnBothOperands).Count > 0)
                            {
                                condbox = true;
                                break;
                            }
                        }
                    }
                    if (condbox) continue;
                }
                bool intersectedWithBound = false;
                if (Boundary.GetClosestPointTo(carsc.GetRecCentroid(), false).DistanceTo(carsc.GetRecCentroid()) < DisVertCarLength)
                {
                    if (carsc.Intersect(Boundary, Intersect.OnBothOperands).Count > 0) intersectedWithBound = true;
                }
                var cond = CarSpatialIndex.SelectCrossingPolygon(carsc).Count == 0
                              && ObstaclesSpatialIndex.SelectCrossingPolygon(carsc).Count == 0
                              && (!IsInAnyPolys(carsc.Centroid(), Laneboxes))
                              && !intersectedWithBound
                              && CheckCarLegal(car);
                if (judge_carmodulebox) cond = cond && (!IsInAnyPolys(carsc.GetRecCentroid(), CarModuleBox));
                if (judge_modulebox) cond = cond && (!IsInAnyPolys(carsc.GetRecCentroid(), ModuleBox));
                if (judge_in_obstacles) cond = cond && ObstaclesMPolygonSpatialIndex.SelectCrossingPolygon(carsc).Count == 0;
                if (cond)
                {
                    AddToSpatialIndex(car, ref CarSpatialIndex);
                    CarSpots.Add(car);
                    crs?.Add(car);
                    if (Pillars.Count > 0)
                    {
                        if (car.GeometricExtents.IsPointIn(Pillars[Pillars.Count - 1].GetRecCentroid()))
                        {
                            Pillars.RemoveAt(Pillars.Count - 1);
                        }
                    }
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
                            var pp = ed.StartPoint.TransformBy(Matrix3d.Displacement(-CreateVector(ed).GetNormal() * DisPillarLength));
                            var li = new Line(pp, ed.StartPoint);
                            var lf = CreateLine(li);
                            lf.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisPillarDepth));
                            var pillar = CreatePolyFromPoints(new Point3d[] { li.StartPoint, li.EndPoint, lf.EndPoint, lf.StartPoint });
                            if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                            {
                                bool condg = true;
                                if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 2].IsPointInFast(pillar.GetRecCentroid()))
                                    condg = false;
                                if (condg)
                                {
                                    //AddToSpatialIndex(pillar, ref CarSpatialIndex);
                                    Pillars.Add(pillar);
                                }
                            }
                            if (adjust_pillar_edge)
                            {
                                vec = -vec;
                            }
                            li.Dispose();
                            lf.Dispose();
                        }
                        precar = car;
                    }
                    else
                    {
                        var dist = car.GetRecCentroid().DistanceTo(precar.GetRecCentroid());
                        if (Math.Abs(dist - length_divided - DisPillarLength) < 1 && GeneratePillars)
                        {
                            var ed = seg;
                            if (adjust_pillar_edge)
                            {
                                ed = s;
                                vec = -vec;
                            }
                            var pp = precar.GetClosestPointTo(ed.StartPoint, false);
                            var li = new Line(pp, ed.StartPoint);
                            var lf = CreateLine(li);
                            lf.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisPillarDepth));
                            var pillar = CreatePolyFromPoints(new Point3d[] { li.StartPoint, li.EndPoint, lf.EndPoint, lf.StartPoint });
                            if (Math.Abs(pillar.Area - DisPillarDepth * DisPillarLength) < 1)
                            {
                                AddToSpatialIndex(pillar, ref CarSpatialIndex);
                                Pillars.Add(pillar);
                            }
                            if (adjust_pillar_edge)
                            {
                                vec = -vec;
                            }
                            li.Dispose();
                            lf.Dispose();
                        }
                        else { }
                        precar = car;
                    }
                    if (glastpillar && c == segscount && GeneratePillars)
                    {
                        var ed = seg;
                        if (adjust_pillar_edge)
                        {
                            ed = s;
                            vec = -vec;
                        }
                        var pp = ed.EndPoint.TransformBy(Matrix3d.Displacement(CreateVector(ed).GetNormal() * DisPillarLength));
                        var li = new Line(pp, ed.EndPoint);
                        var lf = CreateLine(li);
                        lf.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisPillarDepth));
                        var pillar = CreatePolyFromPoints(new Point3d[] { li.StartPoint, li.EndPoint, lf.EndPoint, lf.StartPoint });
                        if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                        {
                            bool condg = true;
                            if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 1].IsPointInFast(pillar.GetRecCentroid()))
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
                        li.Dispose();
                        lf.Dispose();
                    }
                }
                carsc.Dispose();
                seg.Dispose();
                s.Dispose();
            }
            segobjs.Dispose();
        }

        /// <summary>
        /// Update boundary edge.
        /// </summary>
        /// <param name="crvs"></param>
        /// <param name="pto"></param>
        /// <param name="vecs"></param>
        /// <param name="bigbox"></param>
        /// <param name="edges"></param>
        /// <param name="disy"></param>
        private void UpdateBoundaryEdge(ref List<Curve> crvs, ref Point3d pto, ref List<Vector3d> vecs, Polyline bigbox, List<Line> edges, double disy)
        {
            Matrix3d mat = Matrix3d.Displacement(Vector3d.YAxis * (disy - 10));
            Matrix3d mat2 = Matrix3d.Displacement(-Vector3d.YAxis * (disy - 10));
            Matrix3d mat3 = Matrix3d.Displacement(new Vector3d(0, 0, 0));
            var v4 = CreateVector(bigbox.GetCenter(), bigbox.GeometricExtents.MinPoint);
            var v5 = CreateVector(bigbox.GetCenter(), bigbox.GeometricExtents.MaxPoint);
            var v6 = CreateVector(bigbox.GetCenter(), new Point3d(bigbox.GeometricExtents.MinPoint.X, bigbox.GeometricExtents.MaxPoint.Y, 0));
            var v7 = CreateVector(bigbox.GetCenter(), new Point3d(bigbox.GeometricExtents.MaxPoint.X, bigbox.GeometricExtents.MinPoint.Y, 0));
            Matrix3d mat4 = Matrix3d.Displacement(v4);
            Matrix3d mat5 = Matrix3d.Displacement(v5);
            Matrix3d mat6 = Matrix3d.Displacement(v6);
            Matrix3d mat7 = Matrix3d.Displacement(v7);

            List<Matrix3d> mats = new List<Matrix3d>() { mat3, mat, mat2 /*, mat4, mat5, mat6, mat7 */};
            List<Curve> crvs2 = new List<Curve>();
            List<Vector3d> vecs2 = new List<Vector3d>();
            Point3d pto2 = new Point3d();
            for (int j = 0; j < mats.Count; j++)
            {
                crvs2 = new List<Curve>();
                pto2 = bigbox.Centroid().TransformBy(mats[j]);
                vecs2 = edges.Select(e => CreateVector(pto2, CreateVector(e).X == 0 ? e.GetCenter().TransformBy(mats[j]) : e.GetCenter()).GetNormal()).ToList();
                List<Curve> inis2 = new List<Curve>();
                IniLanes.ForEach(e => inis2.Add(e.Line));
                inis2.AddRange(Walls);
                for (int i = 0; i < edges.Count; i++)
                {
                    Line l = CreateLineFromStartPtAndVector(pto2, vecs2[i], 500000);
                    double mindis = 9999999;
                    Curve crv = new Line();
                    foreach (var c in inis2)
                    {
                        var pss = l.Intersect(c, Intersect.OnBothOperands);
                        if (pss.Count > 0 && pss[0].DistanceTo(l.StartPoint) < mindis)
                        {
                            mindis = pss[0].DistanceTo(l.StartPoint);
                            crv = c;
                        }
                    }
                    crvs2.Add(crv);
                    l.Dispose();
                }
                crvs2 = crvs2.Distinct().Where(e => e.GetLength() > 1).ToList();
                if (crvs2.Count > crvs.Count)
                {
                    crvs = crvs2;
                    pto = pto2;
                    vecs = vecs2;
                }
            }
        }

        /// <summary>
        /// Generate car spots unidirectionally in the smallest partition unit.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="vec"></param>
        /// <param name="obstacles"></param>
        /// <param name="boundobstacles"></param>
        /// <param name="length_module_used"></param>
        private void LayoutOneDirection(Line lane, Vector3d vec, List<Polyline> obstacles, List<Polyline> boundobstacles, ref double length_module_used)
        {
            List<Line> ilanes = new List<Line>();
            var lanev = CreateLine(lane);
            lanev.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (DisLaneWidth / 2 + DisVertCarWidth * MinCountAllowGVertCarOnLine)));
            var points = new List<Point3d>();
            var plv = CreatePolyFromPoints(new Point3d[] { lane.StartPoint, lane.EndPoint, lanev.EndPoint, lanev.StartPoint });
            var crossedv = ObstaclesSpatialIndex.SelectCrossingPolygon(plv).Cast<Polyline>().ToList();
            crossedv.AddRange(boundobstacles.Where(e => e.Intersect(plv, Intersect.OnBothOperands).Count > 0));
            foreach (var cross in crossedv)
            {
                points.AddRange(cross.Vertices().Cast<Point3d>().Where(p => plv.Contains(p)));
                points.AddRange(cross.Intersect(plv, Intersect.OnBothOperands));
                points.AddRange(IniBoundary.Intersect(plv, Intersect.OnBothOperands));
            }
            points = points.Select(p => lane.GetClosestPointTo(p, false)).ToList();
            var ls = SplitLine(lane, points).Where(l => l.Length > DisModulus);
            ls = ls.Where(l =>
              {
                  var bf = CreatePolyFromPoints(new Point3d[] { l.StartPoint,l.EndPoint,
                        l.EndPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (DisLaneWidth / 2 + DisVertCarWidth * MinCountAllowGVertCarOnLine))),
                        l.StartPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (DisLaneWidth / 2 + DisVertCarWidth * MinCountAllowGVertCarOnLine)))});
                  bf.Scale(bf.GetRecCentroid(), ScareFactorForCollisionCheck);
                  var condb = ObstaclesMPolygonSpatialIndex.SelectCrossingPolygon(bf).Count == 0;
                  bf.Dispose();
                  var condc = IniBoundary.Contains(l.GetCenter().TransformBy(Matrix3d.Displacement(vec.GetNormal() * (DisLaneWidth / 2 + DisVertCarWidth * MinCountAllowGVertCarOnLine))));
                  return condb && condc;
              });
            ls.ForEach(e =>
               {
                   var segs = new DBObjectCollection();
                   DivideCurveByLength(e, DisModulus, ref segs);
                   ilanes.AddRange(segs.Cast<Line>().Where(t => Math.Abs(t.Length - DisModulus) < 1));
               });
            int modulecount = ilanes.Count;
            //int modulecount = ((int)Math.Floor(lane.Length / DisModulus));
            int vertcount = ((int)Math.Floor((lane.Length - modulecount * DisModulus) / DisVertCarWidth));
            //for (int i = 0; i < modulecount; i++)
            //{
            //    var ptbasestart = lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane).GetNormal() * DisModulus * i));
            //    var ptbaseend = lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane).GetNormal() * DisModulus * (i + 1)));
            //    Line unitbase = new Line(ptbasestart, ptbaseend);
            //    ilanes.Add(unitbase);
            //}
            PMModlues pMModlues = ConstructPMModules(vec, ilanes, boundobstacles);
            for (int i = 0; i < vertcount; i++)
            {
                var test = UpdataPMModlues(pMModlues, boundobstacles);
                if (test.Count >= pMModlues.Count)
                {
                    pMModlues = test;
                }
                else continue;
            }
            PModuleBox.AddRange(pMModlues.Bounds);
            ilanes.ForEach(e => e.Dispose());
            List<Line> restsegs = new List<Line>();
            if (pMModlues.Bounds.Count > 0)
                restsegs = SplitLine(lane, pMModlues.Bounds).Cast<Line>().Where(e => ClosestPointInCurves(e.GetCenter(), pMModlues.Bounds) > 1).ToList();
            else restsegs.Add(lane);
            restsegs = restsegs.Where(e => e.Length >= DisVertCarWidth).ToList();
            GenerateRestVertAndParallelSpots(restsegs, vec, boundobstacles);
            restsegs.ForEach(e => e.Dispose());
            GetCarSpots(ref pMModlues, ref CarSpatialIndex, ObstaclesSpatialIndex, ModuleBox, Boundary);
            length_module_used = pMModlues.Bounds.Count * DisModulus;
        }

        /// <summary>
        /// Generate car spots in the pmmodule.
        /// </summary>
        /// <param name="carSpacilaIndex"></param>
        /// <param name="obsSpacilaIndex"></param>
        /// <param name="ModuleBox"></param>
        /// <param name="boundary"></param>
        /// <returns></returns>
        private void GetCarSpots(ref PMModlues pMModlues, ref ThCADCoreNTSSpatialIndex carSpacilaIndex, ThCADCoreNTSSpatialIndex obsSpacilaIndex, List<Polyline> ModuleBox, Polyline boundary)
        {
            List<Polyline> spots = new List<Polyline>();
            foreach (var pl in pMModlues.Bounds)
            {
                var objs = new DBObjectCollection();
                pl.Explode(objs);
                var segs = objs.Cast<Line>().ToArray();
                var a = segs[1];
                var b = segs[3];
                b.ReverseCurve();
                List<Line> lines = new List<Line>() { b };
                var vec = CreateVector(a.GetClosestPointTo(b.GetCenter(), true), b.GetCenter()).GetNormal() * DisVertCarLength;
                List<Vector3d> vecs = new List<Vector3d>() { -vec };
                var tb = CreateLine(b);
                //UnifyLaneDirection(ref tb, IniLanes);
                //tb.TransformBy(Matrix3d.Displacement(CreateVector(b).GetNormal() * DisLaneWidth / 2));
                tb = new Line(tb.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(b).GetNormal() * DisLaneWidth / 2)), tb.EndPoint);
                GenerateCarsAndPillarsForEachLane(tb, -vec, DisVertCarWidth, DisVertCarLength, false, false, true, true, false, true);
                var plb = CreateLine(b);
                plb.TransformBy(Matrix3d.Displacement(-vec.GetNormal() * DisCarAndHalfLane));
                var plbp = CreatePolyFromPoints(new Point3d[] { b.StartPoint, b.EndPoint, plb.EndPoint, plb.StartPoint });
                AddToSpatialIndex(plbp, ref carSpacilaIndex);
                ModuleBox.Add(plbp);
                objs.Dispose();
                tb.Dispose();
                plb.Dispose();
            }
        }

        /// <summary>
        /// Generate the rest car spots of intergral modules in the smallest partition unit. 
        /// </summary>
        /// <param name="restsegs"></param>
        /// <param name="vec"></param>
        /// <param name="boundobstacles"></param>
        /// <returns></returns>
        private void GenerateRestVertAndParallelSpots(List<Line> restsegs, Vector3d vec, List<Polyline> boundobstacles)
        {
            List<Polyline> respots = new List<Polyline>();
            List<Line> restls = new List<Line>();
            for (int i = 0; i < restsegs.Count; i++)
            {
                var k = restsegs[i];
                UnifyLaneDirection(ref k, IniLanes);
                restsegs[i] = k;
            }
            foreach (var e in restsegs)
            {
                e.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisLaneWidth / 2));
                restls.Add(e);
                var l = CreateLine(e);
                l.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisVertCarLength));
                var validvertlines = SplitLineByObstacles(e, l);
                foreach (var vl in validvertlines)
                {
                    GenerateCarsAndPillarsForEachLane(vl, vec, DisVertCarWidth, DisVertCarLength, true, true, true, false, true, true, true, true, true, respots);
                }
                validvertlines.ForEach(f => f.Dispose());
                l.Dispose();
            }
            foreach (var e in restls)
            {         
                var l = CreateLine(e);
                l.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisParallelCarWidth));
                var validvertlines = SplitLineByObstacles(e, l);
                foreach (var vlk in validvertlines)
                {
                    if (vlk.Length < DisVertCarLength) continue;
                    var lis = SplitLine(vlk, respots).Cast<Line>().Where(f => f.Length >= DisVertCarLength).Where(f => !IsInAnyBoxes(f.GetCenter(), respots)).ToList();
                    foreach (var vl in lis)
                    {
                        GenerateCarsAndPillarsForEachLane(vl, vec, DisParallelCarLength, DisParallelCarWidth, true, true,true, false, false, true, true, true);
                    }
                    lis.ForEach(f => f.Dispose());
                }
                validvertlines.ForEach(f => f.Dispose());
                l.Dispose();
            }
        }

        /// <summary>
        /// Split line by obstacles in the smallest partition unit.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="oriline"></param>
        /// <returns></returns>
        private List<Line> SplitLineByObstacles(Line line, Line oriline)
        {
            var pl = CreatePolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, oriline.EndPoint, oriline.StartPoint });
            pl.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pl.GetRecCentroid()));
            List<Point3d> points = new List<Point3d>();
            var crossedobjs = ObstaclesSpatialIndex.SelectCrossingPolygon(pl).Cast<Polyline>().ToList();
            crossedobjs.AddRange(Obstacles.Where(e => pl.GeometricExtents.IsPointIn(e.Centroid())));
            ThCADCoreNTSSpatialIndex carindexes = new ThCADCoreNTSSpatialIndex(CarSpots.ToCollection());
            crossedobjs.AddRange(carindexes.SelectCrossingPolygon(pl).Cast<Polyline>().ToList());
            foreach (var obj in crossedobjs)
            {
                points.AddRange(obj.Vertices().Cast<Point3d>().Where(e => pl.IsPointInFast(e)));
                points.AddRange(obj.Intersect(pl, Intersect.OnBothOperands));
            }
            points.AddRange(IniBoundary.Intersect(pl, Intersect.OnBothOperands));
            points = points.Where(e => pl.GeometricExtents.IsPointIn(e)).ToList();
            points = points.Select(e => line.GetClosestPointTo(e, false)).Distinct().ToList();
            //RemoveDuplicatePts(points);
            carindexes.Dispose();
            var res = SplitLine(line, points).Where(e => e.Length > DisVertCarWidth - 1).ToList();
            //res = res.Where(e => !IsInAnyPolys(e.GetCenter(),Obstacles)).ToList();
            res = res.Where(e =>
            {
                var pltmp = CreatePolyFromPoint(e.GetCenter());
                var resin = ObstaclesMPolygonSpatialIndex.SelectCrossingPolygon(pltmp).Count == 0;
                pltmp.Dispose();
                return resin;
            }).ToList();
            pl.Dispose();
            return res;
        }

        /// <summary>
        /// Judge if the lane to be generated is nececcary to move to the specific place near the building and return the distance.
        /// </summary>
        /// <param name="boxes"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        private double IsUnderAndNearObstacles(List<Polyline> boxes, Line lane)
        {
            double distance = 9999999;
            foreach (var box in boxes)
            {
                if (box.Centroid().Y > lane.GetCenter().Y)
                {
                    var p_on_lane = lane.GetClosestPointTo(box.Centroid(), true);
                    var p_on_box = box.GetClosePoint(p_on_lane);
                    double d = p_on_lane.DistanceTo(p_on_box);
                    if (d < DisModulus + DisLaneWidth / 2 - 10 && d < distance) distance = d;
                }
            }
            if (distance == 9999999) return -1;
            return distance;
        }

        /// <summary>
        /// Judge if the line is horizontal(not precisely).
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsHorizantal(Line line)
        {
            Vector3d vec = CreateVector(line);
            double x = Math.Abs(vec.X);
            double y = Math.Abs(vec.Y);
            if (x < y) x = 0;
            else y = 0;
            if (y == 0) return true;
            return false;

        }

        /// <summary>
        /// Function to implement the method GenerateAdjLanes.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="isStartpt"></param>
        /// <returns></returns>
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

            var lt = CreateLineFromStartPtAndVector(ps, lane.Vec, MaxDistance);
            var cutters = new List<Polyline>() { Boundary };
            IniLanes.ForEach(e => cutters.Add(CreatePolyFromLine(e.Line)));

            List<Line> segs = new List<Line>();
            Polyline pl = new Polyline();
            Line r = new Line();
            var disu = IsTheAdjLaneUnderAndNearObstacles(BuildingBoxes, lt);
            bool under = false;
            if (disu != -1 && IsHorizantal(lt))
            {
                under = true;
                var underl = CreateLine(lt);
                underl.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * (disu - DisLaneWidth / 2)));
                segs = SplitLine(underl, cutters).Cast<Line>().Where(e => e.Length > 1).ToList();
                segs.OrderBy(e => e.GetCenter().DistanceTo(pt));
                r = segs[0];
                pl = r.Buffer(DisLaneWidth / 2);
                pl.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pl.Centroid()));
                var crossedunder = ObstaclesSpatialIndex.SelectCrossingPolygon(pl);
                if (crossedunder.Count > 0) return false;
            }
            else
            {
                segs = SplitLine(lt, cutters).Cast<Line>().Where(e => e.Length > 1).ToList();
                segs.OrderBy(e => e.GetCenter().DistanceTo(pt));
                r = segs[0];
                pl = r.Buffer(DisLaneWidth / 2);
                var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(pl);
                if (crossed.Count > 0) return false;
            }
            pl.Dispose();
            var cond_allow_connecteddouble = true;
            if (ClosestPointInVertCurves(r.StartPoint, r, IniLaneLines) < 10 && ClosestPointInVertCurves(r.EndPoint, r, IniLaneLines) < 10 && r.Length < LengthCanGAdjLaneConnectedDouble)
                cond_allow_connecteddouble = false;
            Vector3d v = new Vector3d();
            v = CreateVector(r).GetPerpendicularVector().GetNormal();
            var p = r.StartPoint.TransformBy(Matrix3d.Displacement(v));
            if (lane.Line.GetClosestPointTo(p, true).DistanceTo(pt) > DisCarAndHalfLane) v = -v;
            var planes = IniLanes.Select(e => e.Line).Where(e => IsParallelLine(lane.Line, e)).Where(e => e.Length > r.Length / 2).ToList();
            var dis = ClosestPointInCurves(r.GetCenter(), planes);
            var distmp = ClosestPointInCurves(r.GetCenter(), IniLanes.Select(e => e.Line).ToList());
            if (dis >= DisModulus / 2 && distmp > 1 && !IsInAnyPolys(r.GetCenter(), IntegralModules.Select(e => e.Box).ToList())
                && cond_allow_connecteddouble)
            {
                Vector3d vec = v;
                if (under) vec = -Vector3d.YAxis;
                Lane lanec = new Lane(r, vec);
                if (!under) lanec.IsAdjLane = true;
                GSingleLanes.Add(lanec);
                IniLanes.Add(lanec);
                generate_adj_lanes = true;
                IniLaneLines.Add(r);
                return generate_adj_lanes;
            }
            lt.Dispose();
            if (segs.Count > 1)
            {
                for (int i = 1; i < segs.Count; i++) segs[i].Dispose();
            }
            
            return generate_adj_lanes;
        }

        /// <summary>
        /// Judge if the adjacent lane to be generated is nececcary to move to the specific place near the building and return the distance.
        /// </summary>
        /// <param name="boxes"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        private double IsTheAdjLaneUnderAndNearObstacles(List<Polyline> boxes, Line lane)
        {
            double distance = 9999999;
            foreach (var box in boxes)
            {
                if (box.Centroid().Y > lane.GetCenter().Y)
                {
                    var p_on_lane = lane.GetClosestPointTo(box.Centroid(), true);
                    var p_on_box = box.GetClosePoint(p_on_lane);
                    double d = p_on_lane.DistanceTo(p_on_box);
                    if (d < distance) distance = d;
                }
            }
            if (distance == 9999999) return -1;
            return distance;
        }

        /// <summary>
        /// Generate car spots on the single vertical direction on the first stage.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="vec"></param>
        private void GenerateVertCars(Line lane, Vector3d vec)
        {
            var disstart = ClosestPointInCurves(lane.StartPoint, IniLaneLines);
            var disend = ClosestPointInCurves(lane.EndPoint, IniLaneLines);
            if (disstart < DisLaneWidth / 2)
                lane = new Line(lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.StartPoint, lane.EndPoint).GetNormal() * (DisLaneWidth / 2 - disstart))), lane.EndPoint);
            if (disend < DisLaneWidth / 2)
                lane = new Line(lane.StartPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.EndPoint, lane.StartPoint).GetNormal() * (DisLaneWidth / 2 - disend))));
            GenerateCarsAndPillarsForEachLane(lane, vec, DisVertCarWidth, DisVertCarLength, false, false, true, false, false, true);
            lane.Dispose();
        }

        /// <summary>
        /// Sub methods of generating cars on one lane on one direction.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="vec"></param>
        /// <param name="cont"></param>
        /// <param name="skip"></param>
        private void generate_cars_in_single_dir(Line offset, Vector3d vec, ref bool cont, ref bool skip, double length_offset, double length_divided)
        {
            var disstart = ClosestPointInVertCurves(offset.StartPoint, offset, IniLaneLines);
            var disend = ClosestPointInVertCurves(offset.EndPoint, offset, IniLaneLines);
            if (disstart < DisLaneWidth / 2)
            {
                var p = offset.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(offset.StartPoint, offset.EndPoint).GetNormal() * (DisLaneWidth / 2 - disstart)));
                if (offset.GetClosestPointTo(p, false).DistanceTo(p) > 1) skip = true;
                else offset = new Line(p, offset.EndPoint);
            }
            if (disend < DisLaneWidth / 2)
            {
                var p = offset.EndPoint.TransformBy(Matrix3d.Displacement(CreateVector(offset.EndPoint, offset.StartPoint).GetNormal() * (DisLaneWidth / 2 - disend)));
                if (offset.GetClosestPointTo(p, false).DistanceTo(p) > 1) skip = true;
                offset = new Line(offset.StartPoint, p);
            }
            if (offset.Length < DisVertCarWidth)
            {
                cont = true;
                return;
            }
            var inioffset = CreateLine(offset);
            offset.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisLaneWidth / 2));
            var pl = CreatePolyFromPoints(new Point3d[] { inioffset.StartPoint, inioffset.EndPoint, offset.EndPoint, offset.StartPoint });
            var plsc = pl.Clone() as Polyline;
            plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plsc.GetRecCentroid()));
            if (NewBound.Intersect(plsc, Intersect.OnBothOperands).Count > 0 || NewBound.IsPointInFast(plsc.GetRecCentroid())) return;

            offset = CreateLine(inioffset);
            inioffset = CreateLine(offset);
            offset.TransformBy(Matrix3d.Displacement(vec.GetNormal() * length_offset));
            pl = CreatePolyFromPoints(new Point3d[] { inioffset.StartPoint, inioffset.EndPoint, offset.EndPoint, offset.StartPoint });
            plsc = pl.Clone() as Polyline;
            plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plsc.Centroid()));

            if (/*NewBound.Intersect(plsc, Intersect.OnBothOperands).Count > 0 || */NewBound.IsPointInFast(plsc.GetRecCentroid())) return;

            if (ObstaclesSpatialIndex.SelectCrossingPolygon(plsc).Count > 0) skip = true;
            if (Boundary.IsPointInFast(pl.Centroid()) && pl.Area > DisVertCarWidth * DisCarAndHalfLane * 2
                && /*plsc.Intersect(Boundary, Intersect.OnBothOperands).Count == 0 &&*/ (!skip))
            {
                var crossed = CarSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList();
                crossed.AddRange(ObstaclesSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList());
                if (true)
                {
                    var lt = CreateLine(inioffset);
                    lt.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisLaneWidth / 2));
                    var validvertlines = SplitLineByObstacles(lt, offset);
                    foreach (var vl in validvertlines)
                    {
                        GenerateCarsAndPillarsForEachLane(vl, vec, length_divided, length_offset - DisLaneWidth / 2, true, true, true, false, false, true);
                    }
                    validvertlines.ForEach(f => f.Dispose());
                    lt.Dispose();
                }
            }
            inioffset.Dispose();
            offset.Dispose();
            pl.Dispose();
            plsc.Dispose();
        }

        /// <summary>
        /// Judge if the car is legal(collision).
        /// </summary>
        /// <param name="car"></param>
        /// <returns></returns>
        private bool CheckCarLegal(Polyline car)
        {
            var pl = car.Clone() as Polyline;
            pl.Scale(pl.Centroid(), ScareFactorForCollisionCheck);
            if (pl.Intersect(Boundary, Intersect.OnBothOperands).Count > 0)
            {
                pl.Dispose();
                return false;
            }
            pl.Dispose();
            return true;
        }

        /// <summary>
        /// Unify lane direction.
        /// </summary>
        /// <param name="lane"></param>
        private static void UnifyLaneDirection(ref Line lane, List<ParkingPartition.Lane> iniLanes)
        {
            var line = CreateLine(lane);
            var lanes = iniLanes.Select(e => e.Line).Where(e => IsPerpLine(line, e)).ToList();
            if (lanes.Count > 0)
            {
                if (ClosestPointInCurves(line.EndPoint, lanes) < 1 && ClosestPointInCurves(line.StartPoint, lanes) < 1)
                {
                    if (line.StartPoint.X - line.EndPoint.X > 1000) line.ReverseCurve();
                    else if (lanes.Count == 0 && line.StartPoint.Y - line.EndPoint.Y > 1000) line.ReverseCurve();
                }
                else if (ClosestPointInCurves(line.EndPoint, lanes) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.StartPoint, lanes) > DisCarAndHalfLane + 1) line.ReverseCurve();
                else if (ClosestPointInCurves(line.EndPoint, lanes) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.StartPoint, lanes) < DisCarAndHalfLane + 1
                    && ClosestPointInCurves(line.EndPoint, lanes) < ClosestPointInCurves(line.StartPoint, lanes)) line.ReverseCurve();
            }
            else
            {
                if (line.StartPoint.X - line.EndPoint.X > 1000) line.ReverseCurve();
                else if (lanes.Count == 0 && line.StartPoint.Y - line.EndPoint.Y > 1000) line.ReverseCurve();
            }
            lane = line;
        }

        public static void SortLaneByDirection(List<Lane> lanes, int mode)
        {
            var comparer = new LaneComparer(mode);
            lanes.Sort(comparer);
        }
        public class LaneComparer : IComparer<Lane>
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
                    if (a.Line.Length == b.Line.Length) return 0;
                    else if (a.Line.Length > b.Line.Length) return -1;
                    else return 1;
                }
                else if (Mode == 1)
                {
                    if (IsHorizontalLine(a.Line) && !IsHorizontalLine(b.Line)) return -1;
                    else if (!IsHorizontalLine(a.Line) && IsHorizontalLine(b.Line)) return 1;
                    else
                    {
                        if (a.Line.Length == b.Line.Length) return 0;
                        else if (a.Line.Length > b.Line.Length) return -1;
                        else return 1;
                    }
                }
                else if (Mode == 2)
                {
                    if (IsVerticalLine(a.Line) && !IsVerticalLine(b.Line)) return -1;
                    else if (!IsVerticalLine(a.Line) && IsVerticalLine(b.Line)) return 1;
                    else
                    {
                        if (a.Line.Length == b.Line.Length) return 0;
                        else if (a.Line.Length > b.Line.Length) return -1;
                        else return 1;
                    }
                }
                else return 0;
            }
        }


    }
}