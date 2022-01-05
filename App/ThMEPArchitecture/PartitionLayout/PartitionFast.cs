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
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public class PartitionFast
    {
        public PartitionFast(List<Polyline> walls, List<Line> iniLanes,
        List<Polyline> obstacles, Polyline boundary, List<Polyline> buildingBox)
        {
            Walls = walls;
            IniLaneLines = iniLanes;
            Obstacles = obstacles;
            Boundary = boundary;
            BuildingBoxes = buildingBox;
            BoundingBox = Boundary.GeometricExtents.ToRectangle();
            MaxDistance = BoundingBox.Length / 2;
            IniBoundary = boundary;
            Countinilanes = iniLanes.Count;
            InitialzeLanes(iniLanes, boundary);
            CheckObstacles();
            CheckBuildingBoxes();
        }
        public List<Polyline> Walls;
        public List<Lane> IniLanes = new List<Lane>();
        public List<Polyline> Obstacles;
        public Polyline Boundary;
        public ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        private List<Polyline> CarSpots = new List<Polyline>();
        private List<Polyline> CarModuleBox = new List<Polyline>();
        public ThCADCoreNTSSpatialIndex MoudleSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        private List<Line> IniLaneLines;
        private List<Polyline> BuildingBoxes;
        private Polyline BoundingBox;
        private double MaxDistance;
        private ThCADCoreNTSSpatialIndex CarSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        private List<Polyline> ModuleBox = new List<Polyline>();
        private List<Polyline> Laneboxes = new List<Polyline>();
        private Polyline IniBoundary = new Polyline();
        private int Countinilanes = 0;
        private List<Polyline> FirstStageCarBoxes = new List<Polyline>();
        private List<Lane> GSingleLanes = new List<Lane>();
        private Polyline NewBound = new Polyline();
        private List<Curve> NewBoundEdges = new List<Curve>();
        private List<Polyline> PModuleBox = new List<Polyline>();

        const double DisLaneWidth = 5500;
        const double DisCarLength = 5100;
        const double DisCarWidth = 2400;
        const double DisCarAndHalfLane = DisLaneWidth / 2 + DisCarLength;
        const double DisModulus = DisCarAndHalfLane * 2;

        const double LengthCanGIntegralModules = 3 * DisCarWidth + DisLaneWidth / 2;
        const double ScareFactorForCollisionCheck = 0.99;

        const double AreaSpotFactor = 30 * 1000 * 1000;


        public int CalCarSpotsFastly()
        {
            double obstacleArea = 0;
            Obstacles = Obstacles.Where(e => e.Closed).ToList();
            Obstacles.ForEach(o => obstacleArea += o.Area);
            return Math.Max(0, ((int)Math.Floor((Boundary.Area - obstacleArea) / AreaSpotFactor)));
        }

        /// <summary>
        /// Construct lane class from lines infos input.
        /// </summary>
        /// <param name="iniLanes"></param>
        /// <param name="boundary"></param>
        private void InitialzeLanes(List<Line> iniLanes, Polyline boundary)
        {
            foreach (var e in iniLanes)
            {
                var vec = CreateVector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!boundary.IsPointIn(pt))
                {
                    vec = -vec;
                }
                IniLanes.Add(new Lane(e, vec));
            }
        }

        /// <summary>
        /// Judge if it is legal building box.
        /// </summary>
        private void CheckBuildingBoxes()
        {
            var points = new List<Point3d>();
            Obstacles.ForEach(o => points.AddRange(o.Vertices().Cast<Point3d>()));
            for (int i = 0; i < BuildingBoxes.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < points.Count; j++)
                {
                    if (BuildingBoxes[i].GeometricExtents.IsPointIn(points[j]))
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;
                BuildingBoxes.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// Judge if it is legal obstacle.
        /// </summary>
        private void CheckObstacles()
        {
            Obstacles = Obstacles.Where(e => e.Intersect(Boundary, Intersect.OnBothOperands).Count > 0 || Boundary.IsPointIn(e.GetCenter())).ToList();
        }

        /// <summary>
        /// Class lane.
        /// </summary>
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
            public double RestLength;
        }
    }
}