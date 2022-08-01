using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.Sprinkler.Data;


namespace ThMEPWSS.SprinklerDim.Data
{
    public class ThSprinklerDimDataProcessService
    {
        private double LineTol = 1;
        //----input
        public List<ThIfcFlowSegment> TchPipeData { private get; set; } = new List<ThIfcFlowSegment>();
        public List<Point3d> SprinklerPt { get; set; } = new List<Point3d>();
        public List<ThExtractorBase> InputExtractors { get; set; }

        //---private
        private List<Entity> WallData { get; set; } = new List<Entity>(); //mpolygon //polyline
        private List<Entity> RoomData { get; set; } = new List<Entity>(); //mpolygon //polyline

        //----output
        public List<Line> TchPipe { get; private set; } = new List<Line>();
        public List<Polyline> Column { get; set; } = new List<Polyline>();
        private List<Polyline> Wall { get; set; } = new List<Polyline>(); //mpolygon //polyline
        private List<Polyline> Room { get; set; } = new List<Polyline>(); //mpolygon //polyline

        public void ProcessData()
        {
            ProcessArchitechData();
            RemoveDuplicateSprinklerPt();
            CreateTchPipe();
            ProjectOntoXYPlane();
        }


        public void CreateTchPipe()
        {
            var line = TchPipeData.Select(o => o.Outline).OfType<Line>().Where(x => x.Length >= LineTol);
            TchPipe.AddRange(line);
        }
        public void RemoveDuplicateSprinklerPt()
        {
            SprinklerPt = SprinklerPt.Distinct().ToList();
        }

        public void ProjectOntoXYPlane()
        {
            TchPipe.ForEach(x => x.ProjectOntoXYPlane());
            SprinklerPt = SprinklerPt.Select(x => new Point3d(x.X, x.Y, 0)).ToList();
            Column.ForEach(x => x.ProjectOntoXYPlane());
            Wall.ForEach(x => x.ProjectOntoXYPlane());
            Room.ForEach(x => x.ProjectOntoXYPlane());
        }

        public void ProcessArchitechData()
        {
            if (InputExtractors != null && InputExtractors.Count() > 0)
            {
                var architectureWallExtractor = InputExtractors.Where(o => o is ThSprinklerArchitectureWallExtractor).First() as ThSprinklerArchitectureWallExtractor;
                var shearWallExtractor = InputExtractors.Where(o => o is ThSprinklerShearWallExtractor).First() as ThSprinklerShearWallExtractor;
                var columnExtractor = InputExtractors.Where(o => o is ThSprinklerColumnExtractor).First() as ThSprinklerColumnExtractor;
                var roomExtractor = InputExtractors.Where(o => o is ThSprinklerRoomExtractor).First() as ThSprinklerRoomExtractor;

                WallData.AddRange(architectureWallExtractor.Walls);
                WallData.AddRange(shearWallExtractor.Walls);
                Column.AddRange(columnExtractor.Columns);
                roomExtractor.Rooms.ForEach(x => RoomData.Add(x.Boundary as Entity));

                Wall.AddRange(WallData.OfType<Polyline>());
                Wall.AddRange(WallData.OfType<MPolygon>().Select(x => x.Shell()));
                Wall.AddRange(WallData.OfType<MPolygon>().SelectMany(x => x.Holes()));

                Room.AddRange(RoomData.OfType<Polyline>());
                Room.AddRange(RoomData.OfType<MPolygon>().Select(x => x.Shell()));
                Room.AddRange(RoomData.OfType<MPolygon>().SelectMany(x => x.Holes()));
            }
        }

        public void Print()
        {
            DrawUtils.ShowGeometry(TchPipe, "l0Pipe", 140);
            SprinklerPt.ForEach(x => DrawUtils.ShowGeometry(x, "l0sprinkler", 3));

            DrawUtils.ShowGeometry(Wall, "l0wall", 1);
            DrawUtils.ShowGeometry(Column, "l0column", 3);
            DrawUtils.ShowGeometry(Room, "l0room", 30);

        }
    }
}
