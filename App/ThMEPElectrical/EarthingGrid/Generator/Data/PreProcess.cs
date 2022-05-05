using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPElectrical.EarthingGrid.Data;
using ThMEPElectrical.EarthingGrid.Service;
using ThMEPElectrical.EarthingGrid.Generator.Connect;

namespace ThMEPElectrical.EarthingGrid.Generator.Data
{
    class PreProcess
    {
        //private DBObjectCollection Beams = new DBObjectCollection();
        private DBObjectCollection Columns = new DBObjectCollection();
        private DBObjectCollection Conductors = new DBObjectCollection();
        //private DBObjectCollection Shearwalls  = new DBObjectCollection();
        private DBObjectCollection MainBuildings = new DBObjectCollection();
        private DBObjectCollection ConductorWires = new DBObjectCollection();
        private DBObjectCollection ArchitectOutlines = new DBObjectCollection();
        //private List<Tuple<Point3d, Point3d>> BeamCenterLinePts = new List<Tuple<Point3d, Point3d>>();
        private Dictionary<Polyline, List<Polyline>> buildingWithWalls = new Dictionary<Polyline, List<Polyline>>();

        public List<Polyline> outlines = new List<Polyline>();
        public HashSet<Polyline> buildingOutline = new HashSet<Polyline>();
        public HashSet<Point3d> columnPts = new HashSet<Point3d>();
        public HashSet<Polyline> innOutline = new HashSet<Polyline>();
        public HashSet<Polyline> extOutline = new HashSet<Polyline>();

        public Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts = new Dictionary<Polyline, HashSet<Point3d>>();
        public Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts = new Dictionary<Polyline, HashSet<Point3d>>();
        public Dictionary<Point3d, HashSet<Point3d>> conductorGraph = new Dictionary<Point3d, HashSet<Point3d>>();

        public PreProcess(ThEarthingGridDatasetFactory dataset)
        {
            Columns = dataset.Columns;
            Conductors = dataset.Conductors;
            //Shearwalls = dataset.Shearwalls;
            MainBuildings = dataset.MainBuildings;
            ConductorWires = dataset.ConductorWires;
            ArchitectOutlines = dataset.ArchitectOutlines;
            //BeamCenterLinePts = dataset.BeamCenterLinePts;

            var group = new ThShearwallGroupService(dataset.Shearwalls, dataset.MainBuildings);
            buildingWithWalls = group.Group();
            buildingOutline = buildingWithWalls.Keys.ToHashSet();
        }

        public void Process()
        {
            //生成outlines
            foreach(var outline in MainBuildings.Cast<Polyline>())
            {
                outlines.Add(outline);
                innOutline.Add(outline);
            }
            foreach (var outline in ArchitectOutlines.Cast<Polyline>())
            {
                outlines.Add(outline);
                extOutline.Add(outline);
                buildingWithWalls.Add(outline, new List<Polyline>{ outline });
            }

            //输入墙、外边框，获得外边框对应的墙点/边界点，获得外边框对应的近点，点集
            var dataProcess = new DataProcess(buildingWithWalls, outlines, Columns, Conductors, ConductorWires);
            dataProcess.Process(ref outlinewithBorderPts, ref outlinewithNearPts, ref columnPts, ref conductorGraph);
        }
    }
}
