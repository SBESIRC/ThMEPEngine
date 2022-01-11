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
        public ParkingPartitionBackup()
        {

        }
        public ParkingPartitionBackup(List<Polyline> walls, List<Line> iniLanes,
        List<Polyline> obstacles, Polyline boundary, bool gpillars = true)
        {
            GeneratePillars = gpillars;
            Walls = walls;
            Obstacles = obstacles;
            Boundary = boundary;
            BoundingBox = Boundary.GeometricExtents.ToRectangle();
            MaxLength = BoundingBox.Length / 2;
            InitialzeDatas(iniLanes);
        }

        private bool GeneratePillars = false;
        public List<Polyline> Walls;
        public List<Polyline> Obstacles;
        public Polyline Boundary;
        private Polyline BoundingBox;
        private double MaxLength;
        public ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        public ThCADCoreNTSSpatialIndex CarBoxesSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public List<Lane> IniLanes = new List<Lane>();
        private List<Polyline> CarSpots = new List<Polyline>();
        private List<Polyline> Pillars = new List<Polyline>();
        private List<Polyline> CarBoxes = new List<Polyline>();
        private List<Point3d> ObstacleVertexes = new List<Point3d>();

        const double DisPillarLength = 400;
        const double DisPillarDepth = 500;
        const int CountPillarDist = 2;
        const double DisLaneWidth = 5500;
        const double DisCarLength = 5100;
        const double DisCarWidth = 2400;
        const double DisCarAndHalfLane = DisLaneWidth / 2 + DisCarLength;
        const double DisModulus = DisCarAndHalfLane * 2;
        const double LengthCanGIntegralModulesConnectSingle = 3 * DisCarWidth + DisLaneWidth / 2;
        const double LengthCanGIntegralModulesConnectDouble = 4 * DisCarWidth + DisLaneWidth;
        const double LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisCarWidth * 3;
        const double LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisCarWidth * 4;
        const double DisPreventGenerateModuleLane = DisLaneWidth + DisCarLength + DisCarWidth;
        const double DisPreventGenerateAdjLane = DisLaneWidth + DisCarLength;
        const double ScareFactorForCollisionCheck = 0.99;

        const int LayoutMode = ((int)LayoutDirection.VERTICAL);
        enum LayoutDirection : int
        {
            LENGTH = 0,
            HORIZONTAL = 1,
            VERTICAL = 2
        }

        private void InitialzeDatas(List<Line> iniLanes)
        {
            foreach (var e in iniLanes)
            {
                var vec = CreateVector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!Boundary.Contains(pt))
                    vec = -vec;
                IniLanes.Add(new Lane(e, vec));
            }
            Obstacles.ForEach(e => ObstacleVertexes.AddRange(e.Vertices().Cast<Point3d>()));
        }

        public void Dispose()
        {
            Walls?.ForEach(e => e.Dispose());
            Boundary?.Dispose();
            BoundingBox?.Dispose();
            IniLanes?.ForEach(e => e.Line?.Dispose());
            CarSpots?.ForEach(e => e.Dispose());
            Pillars?.ForEach(e => e.Dispose());
            CarBoxes?.ForEach(e => e.Dispose());
        }

        public void Display(string layer = "0", int colorindex = 0)
        {
            CarSpots.Select(e =>
            {
                e.Layer = layer;
                e.ColorIndex = colorindex;
                return e;
            }).AddToCurrentSpace();
            Pillars.Select(e =>
            {
                e.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(15, 240, 206);
                return e;
            }).AddToCurrentSpace();
        }

        public void GenerateParkingSpaces()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                GenerateLanes();
                IniLanes.ForEach(e =>
                {
                    //e.Line.ColorIndex = ((int)ColorIndex.Red);
                    //e.Line.AddToCurrentSpace();
                });
            }
        }

        private void GenerateLanes()
        {
            int count = 0;
            while (true)
            {
                count++;
                if (count > 20) break;
                int lanecount = IniLanes.Count;

                var generate_integral_modules = GenerateIntegralModuleLanes();
                if (generate_integral_modules) continue;

                var generate_adj_lanes = GenerateAdjLanes();
                if (generate_adj_lanes) continue;

                var generate_split_two_vertical_buildings_lanes = GenerateSplitTwoVerticalBuildingsLanes();
                if (generate_split_two_vertical_buildings_lanes) continue;

                if (lanecount == IniLanes.Count) break;
            }
            CarBoxes.ForEach(e =>
            {
                e.ColorIndex = ((int)ColorIndex.Red);
                e.AddToCurrentSpace();
            });
        }

        private bool GenerateIntegralModuleLanes()
        {
            var generate_integral_modules = false;
            IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            //SortModuleLaneByGeneratedLength(IniLanes, DisModulus, LengthCanGIntegralModulesConnectSingle, LengthCanGIntegralModulesConnectDouble);
            for (int i = 0; i < IniLanes.Count; i++)
            {
                generate_integral_modules = GenerateModuLeLaneGenerally(IniLanes[i], i, DisModulus, LengthCanGIntegralModulesConnectSingle, LengthCanGIntegralModulesConnectDouble);
                if (generate_integral_modules) break;
            }
            return generate_integral_modules;
        }

        private bool GenerateAdjLanes()
        {
            var generate_adj_lanes = false;
            IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            //SortLaneByDirection(IniLanes, LayoutMode);
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i];
                if (lane.Line.Length <= DisCarAndHalfLane) continue;
                var lanes = IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(lane.Line, e)).ToList();
                var distart = ClosestPointInCurves(lane.Line.StartPoint, lanes);
                var disend = ClosestPointInCurves(lane.Line.EndPoint, lanes);
                if (distart > 1 || lanes.Count == 0)
                {
                    var generated = GenerateAdjLanesFunc(lane, true);
                    if (generated)
                    {
                        generate_adj_lanes = true;
                        break;
                    }
                }
                if (disend > 1 || lanes.Count == 0)
                {
                    var generated = GenerateAdjLanesFunc(lane, false);
                    if (generated)
                    {
                        generate_adj_lanes = true;
                        break;
                    }
                }
            }
            return generate_adj_lanes;
        }

        private bool GenerateSplitTwoVerticalBuildingsLanes()
        {
            var generate_split_two_vertical_buildings_lanes = false;
            return generate_split_two_vertical_buildings_lanes;
        }


    }

    public class Lane
    {
        public Lane(Line line, Vector3d vec, bool canBeMoved = true)
        {
            Line = line;
            Vec = vec;
            CanBeMoved = canBeMoved;
        }
        public Line Line;
        public bool CanBeMoved;
        public Vector3d Vec;
        public bool GeneratedAdjLane = false;
    }

    public class CarModules
    {
        public Polyline Box;
        public Line Line;
        public Vector3d Vec;
    }
}