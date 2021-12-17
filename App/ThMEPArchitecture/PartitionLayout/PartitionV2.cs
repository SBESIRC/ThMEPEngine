using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public class PartitionV2
    {
        public PartitionV2(List<Polyline> walls, List<Line> iniLanes,
        List<Polyline> obstacles, Polyline boundary)
        {
            Walls = walls;
            IniLanes = new List<Line>(iniLanes);
            Obstacles = obstacles;
            Boundary = boundary;
            var build = new Extents3d();
            obstacles.ForEach(e => build.AddExtents(e.GeometricExtents));
            BBoundingBox = build.ToRectangle();
            BoundingBox = boundary.GeometricExtents.ToRectangle();
        }
        public List<Polyline> Walls;
        public List<Line> IniLanes;
        public List<Polyline> Obstacles;
        public Polyline Boundary;
        public DBObjectCollection Cutters = new DBObjectCollection();
        public ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        private List<Polyline> CarSpots = new List<Polyline>();
        private List<PartitionUnit> PartitionUnits = new List<PartitionUnit>();
        private Polyline BBoundingBox;
        private Polyline BoundingBox;

        const double DisLaneWidth = 5500;
        const double DisCarLength = 5100;
        const double DisCarWidth = 2400;
        const double DisCarAndHalfLane = DisLaneWidth / 2 + DisCarLength;
        const double DisCarAndLane = DisLaneWidth + DisCarLength;
        const double DisModulus = DisCarAndHalfLane * 2;

        //custom
        const double LengthGenetateHalfLane = 10600;
        const double LengthGenetateModuleLane = 10600;

        public void GenerateParkingSpaces()
        {
            List<PartitionUnit> iniunits = new List<PartitionUnit>();
            SplitPartitionDeeply();
            Boundary.ColorIndex = ((int)ColorIndex.Yellow);
            Boundary.AddToCurrentSpace();
            GenerateSpotsInMinUnit(IniLanes, Walls, Obstacles, ObstaclesSpatialIndex);
        }

        private void GenerateSpotsInMinUnit(List<Line> lines, List<Polyline> walls,
            List<Polyline> obstacles, ThCADCoreNTSSpatialIndex obstaclespatialIndex)
        {

        }

        private void SplitPartitionInitially()
        {
            var bbboxline = BBoundingBox.ExplodeLines().OrderBy(e => e.GetCenter().Y).ToArray()[0];
            bbboxline.TransformBy(Matrix3d.Displacement(new Vector3d(0, -DisLaneWidth / 2, 0)));
            Point3d ptsect = bbboxline.GetCenter();
            Line linevert = LineSDL(ptsect, -Vector3d.YAxis, 100000);
            foreach (var e in IniLanes)
            {
                var ps = linevert.Intersect(e, Intersect.OnBothOperands);
                if (ps.Count > 0)
                {
                    ptsect = ps[0];
                    break;
                }
            }
            if (ptsect.DistanceTo(bbboxline.GetCenter()) < 1)
            {
                foreach (var e in Walls)
                {
                    var ps = linevert.Intersect(e, Intersect.OnBothOperands);
                    if (ps.Count > 0)
                    {
                        ptsect = ps[0];
                        break;
                    }
                }
                if (ptsect.DistanceTo(bbboxline.GetCenter()) < DisCarAndHalfLane - 1)
                    return;
            }
            else
            {
                if (ptsect.DistanceTo(bbboxline.GetCenter()) < DisModulus - 1)
                    return;
            }
            bbboxline.TransformBy(Matrix3d.Scaling(10, bbboxline.GetCenter()));
            bbboxline = SplitCurve(bbboxline, Boundary).Where(e => Boundary.IsPointIn(e.GetCenter()))
               .Select(e => new Line(e.StartPoint, e.EndPoint)).First();
            var pus = SplitPartitionsByLine(Walls.ToArray(), IniLanes.ToArray(), bbboxline, Boundary);
            PartitionUnits.Add(pus[0]);
            IniLanes = pus[1].Lanes.ToList();
            Walls = pus[1].Walls.ToList();
            Boundary = pus[1].Boundary;
        }

        private void SplitPartitionDeeply()
        {
            SplitPartitionForCompleteModules();
        }

        private void SplitPartitionForCompleteModules()
        {
            int count = 0;
            
            while (true)
            {
                count++;
                if (count > 10) break;
                bool found = false;
                IniLanes = IniLanes.OrderByDescending(e => e.Length).ToList();        
                for (int i = 0; i < IniLanes.Count; i++)
                {
                    var ofstlane = new Line(IniLanes[i].StartPoint, IniLanes[i].EndPoint);
                    var ofstlanetest = new Line(IniLanes[i].StartPoint, IniLanes[i].EndPoint);
                    var ofstvec = GetOffetDirection(ofstlane, Boundary);
                    ofstlane.TransformBy(Matrix3d.Displacement(ofstvec * (DisModulus)));
                    ofstlanetest.TransformBy(Matrix3d.Displacement(ofstvec * (DisModulus + DisLaneWidth / 2)));
                    List<Curve> crvs = new List<Curve>();
                    crvs.AddRange(IniLanes);
                    crvs.AddRange(Walls);
                    try
                    {
                        ofstlane.TransformBy(Matrix3d.Scaling(10, ofstlane.GetCenter()));
                        ofstlane = SplitCurve(ofstlane, crvs.ToArray()).Where(e => Boundary.IsPointIn(e.GetCenter())).Cast<Line>()
                            .Where(e => e.Length > 10).ToArray().First();
                        ofstlanetest.TransformBy(Matrix3d.Scaling(10, ofstlanetest.GetCenter()));
                        ofstlanetest = SplitCurve(ofstlanetest, crvs.ToArray()).Where(e => Boundary.IsPointIn(e.GetCenter())).Cast<Line>()
                            .Where(e => e.Length > 10).ToArray().First();
                    }
                    catch
                    {
                        continue;
                    }
                    var pltest = PolyFromPoints(new List<Point3d>() {
                        IniLanes[i].StartPoint,IniLanes[i].EndPoint,ofstlanetest.EndPoint,ofstlanetest.StartPoint,IniLanes[i].StartPoint});
                    pltest.TransformBy(Matrix3d.Scaling(0.99, pltest.Centroid()));
                    if (ObstaclesSpatialIndex.Intersects(pltest)) continue;
                    else
                    {
                        if (Boundary.IsPointIn(ofstlanetest.GetCenter()))
                        {
                            var pus = SplitPartitionsByLine(Walls.ToArray(), IniLanes.ToArray(), ofstlane, Boundary);
                            PartitionUnits.Add(pus[0]);
                            IniLanes = pus[1].Lanes.ToList();
                            Walls = pus[1].Walls.ToList();
                            Boundary = pus[1].Boundary;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found) break;
            }
        }

        private Vector3d GetOffetDirection(Line line, Polyline boundary)
        {
            Vector3d vec = Vector(line).GetPerpendicularVector().GetNormal();
            var pt = line.GetCenter().TransformBy(Matrix3d.Displacement(vec * 10));
            if (boundary.IsPointIn(pt)) return vec;
            else return -vec;
        }

        private List<PartitionUnit> SplitPartitionsByLine(Polyline[] walls, Line[] lanes, Line cutter, Polyline boundary)
        {
            List<PartitionUnit> results = new List<PartitionUnit>();
            var splited_walls = walls.ToList();
            for (int i = 0; i < splited_walls.Count; i++)
            {
                var res = SplitCurve(splited_walls[i], cutter);
                if (res.Length > 1)
                {
                    splited_walls.RemoveAt(i);
                    i--;
                    splited_walls.AddRange(res.Cast<Polyline>());
                }
            }
            var split_lanes = lanes.ToList();
            for (int i = 0; i < split_lanes.Count; i++)
            {
                var res = SplitCurve(split_lanes[i], cutter);
                if (res.Length > 1)
                {
                    split_lanes.RemoveAt(i);
                    i--;
                    split_lanes.AddRange(res.Cast<Line>());
                }
            }
            var lanes_a = split_lanes.Where(e => JudgeCurveDirectionBasedLine(e, cutter) == 0);
            var lanes_b = split_lanes.Where(e => JudgeCurveDirectionBasedLine(e, cutter) == 1);
            var walls_a = splited_walls.Where(e => JudgeCurveDirectionBasedLine(e, cutter) == 0);
            var walls_b = splited_walls.Where(e => JudgeCurveDirectionBasedLine(e, cutter) == 1);
            var la = lanes_a.ToList();
            la.Add(cutter);
            var ba = JoinCurves(walls_a.ToList(), la)[0];
            PartitionUnit pua = new PartitionUnit(la.ToArray(), walls_a.ToArray(), ba);
            var lb = lanes_b.ToList();
            lb.Add(cutter);
            var bb = JoinCurves(walls_b.ToList(), lb)[0];
            PartitionUnit pub = new PartitionUnit(lb.ToArray(), walls_b.ToArray(), bb);
            foreach (var o in Obstacles)
            {
                if (pua.Boundary.IsPointIn(o.GetCenter()))
                {
                    results.Add(pub);
                    results.Add(pua);
                    break;
                }
            }
            if (results.Count == 0)
            {
                results.Add(pua);
                results.Add(pub);
            }
            return results;
        }

        private int JudgeCurveDirectionBasedLine(Curve curve, Line line)
        {
            var pt_on_line = line.GetClosestPointTo(curve.GetCenter(), true);
            var vec = Vector(curve.GetCenter(), pt_on_line);
            if (Math.Abs(vec.X) < 1) vec = new Vector3d(0, vec.Y, 0);
            if (Math.Abs(vec.Y) < 1) vec = new Vector3d(vec.X, 0, 0);
            if (vec.X > 0 || vec.Y > 0)
                return 0;
            return 1;
        }

        private class PartitionUnit
        {
            public PartitionUnit(Line[] lanes, Polyline[] walls, Polyline boundary)
            {
                Lanes = lanes;
                Walls = walls;
                Boundary = boundary;
            }
            public Line[] Lanes { get; set; }
            public Polyline[] Walls { get; set; }
            public bool HasObstacles = false;
            public Polyline Boundary { get; set; }
        }

        private class Lane
        {
            public Lane(Line line)
            {
                Line = line;
            }
            public Line Line;
            public bool CanMove = true;
            public Vector3d Vec;
        }
    }
}
