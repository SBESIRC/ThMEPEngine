using System;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Pipe.Tools;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWTopFloorRecognitionEngine 
    {
        public List<ThWTopFloorRoom> Rooms { get; set; }
        public List<ThIfcRoom> Spaces { get; set; }
        public List<ThIfcRoom> StandardSpaces { get; set; }
        public List<ThIfcRoom> NonStandardSpaces { get; set; }
        public List<BlockReference> blockCollection { get; set; }
        public List<ThWRainPipe> rainPipes { get; set; }
        public List<ThWRoofRainPipe> roofRainPipes { get; set; }
        public List<ThWCondensePipe> condensePipes { get; set; }
        public List<ThWWashingMachine> washmachines { get; set; }
        public List<ThWBasin> basinTools { get; set; }
        public List<ThWFloorDrain> floorDrains { get; set; }
        public List<ThWClosestool> closets { get; set; }
        public ThWTopFloorRecognitionEngine()
        {
            Rooms = new List<ThWTopFloorRoom>();
        }
        public void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWTopFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {                      
                var thisSpaces = Spaces;
                var basepoint = GetBaseCircles(blockCollection);                 
                var compositeroom = Getcompositeroom(database, GetBoundaryVertices(StandardSpaces), thisSpaces).Item1;
                var compositebalconyroom = Getcompositeroom(database, GetBoundaryVertices(StandardSpaces), thisSpaces).Item2;
                var divisionLines = GetLines(blockCollection, thisSpaces);
                Rooms = ThTopFloorRoomService.Build(StandardSpaces, basepoint, compositeroom, compositebalconyroom, divisionLines);        
            }
        }
        public static Point3dCollection GetBoundaryVertices(List<ThIfcRoom> StandardSpaces)
        {
            var Vertices = new Point3dCollection();
            var minpt = new Point3d(double.MinValue,0,0);
            var maxpt = new Point3d(double.MaxValue, 0, 0);
            for (int i=0;i< StandardSpaces.Count;i++)
            {
                Polyline bound = StandardSpaces[i].Boundary as Polyline;
                var minpoint = bound.GeometricExtents.MinPoint;
                var maxpoint = bound.GeometricExtents.MaxPoint;
                if (maxpoint.X> minpt.X)
                {
                    minpt = maxpoint;
                }
                if (minpoint.X < maxpt.X)
                {
                    maxpt = minpoint;
                }
            }            
            Vertices.Add(maxpt);
            Vertices.Add(new Point3d(minpt.X, maxpt.Y, 0));
            Vertices.Add(minpt);
            Vertices.Add(new Point3d(maxpt.X, minpt.Y, 0));
            return Vertices;
        }
        public static List<ThIfcRoom> GetBaseCircles(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcRoom>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("标准层"))
                {
                    var s = new DBObjectCollection();
                    block.Explode(s);
                    List<Circle> circle = new List<Circle>();
                    foreach (var s1 in s)
                    {
                        if (s1.GetType().Name.Contains("Circle"))
                        {
                            Circle baseCircle = s1 as Circle;
                            FloorSpaces.Add(new ThIfcRoom { Boundary = baseCircle });
                        }
                    }
                }
            }
            return FloorSpaces;
        }
        public static List<Curve> GetBoundaryCurves(List<BlockReference> blockCollection)
        {
            var blockCurves = new List<Curve>();
            foreach (BlockReference block in blockCollection)
            {
                blockCurves.Add(ThWPipeOutputFunction.GetBlockBoundary(block));
            }
            return blockCurves;
        }
        private Tuple<List<ThWCompositeRoom>, List<ThWCompositeBalconyRoom>>  Getcompositeroom(Database database, Point3dCollection pts,List<ThIfcRoom> spaces)
        {
            using (ThWCompositeRoomRecognitionEngine compositeRoomRecognitionEngine = new ThWCompositeRoomRecognitionEngine())
            {
                compositeRoomRecognitionEngine.Spaces = spaces;
                compositeRoomRecognitionEngine.rainPipes = rainPipes;
                compositeRoomRecognitionEngine.roofRainPipes = roofRainPipes;
                compositeRoomRecognitionEngine.washmachines = washmachines;
                compositeRoomRecognitionEngine.floorDrains = floorDrains;
                compositeRoomRecognitionEngine.condensePipes = condensePipes;
                compositeRoomRecognitionEngine.closets = closets;
                compositeRoomRecognitionEngine.basinTools = basinTools;
                compositeRoomRecognitionEngine.Recognize(database, pts);
                return Tuple.Create(compositeRoomRecognitionEngine.Rooms, compositeRoomRecognitionEngine.FloorDrainRooms);                  
            }
        }
         private List<Line> GetLines(List<BlockReference> blocks, List<ThIfcRoom> spaces)
        {
            var DivisionLines = new List<Line>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("标准层"))
                {
                    var s = new DBObjectCollection();
                    block.Explode(s);
                    foreach (var s1 in s)
                    {
                        if (s1.GetType().Name.Contains("Line"))
                        {
                            Line divisionLine = s1 as Line;
                            if((divisionLine.StartPoint.X> block.GeometricExtents.MinPoint.X)&&
                                (divisionLine.StartPoint.X < block.GeometricExtents.MaxPoint.X))
                            DivisionLines.Add(divisionLine);
                        }
                    }
                }
            }
            return GetColumnLines(DivisionLines, spaces);
        }
        private static List<Line> GetColumnLines(List<Line> Columns, List<ThIfcRoom> spaces)
        {
            var colunmnLines = new List<Line>();
            foreach(Line column in Columns)
            {
                if((column.StartPoint.X>= GetMinPointX(spaces))&&(column.StartPoint.X <= GetMaxPointX(spaces)))
                {
                    colunmnLines.Add(column);
                }
            }
            return colunmnLines;
        }
        private static double GetMaxPointX(List<ThIfcRoom> spaces)
        {
            double baseX = double.MinValue;
            var maxpoint = Point3d.Origin;
            for (int i=0;i< spaces.Count;i++)
            {
              
                if ((spaces[i].Boundary.GeometricExtents.MinPoint.X + spaces[i].Boundary.GeometricExtents.MaxPoint.X) / 2 > baseX)
                {
                    baseX = (spaces[i].Boundary.GeometricExtents.MinPoint.X + spaces[i].Boundary.GeometricExtents.MaxPoint.X) / 2;                 
                }
            }
            return baseX;
        }
        private static double GetMinPointX(List<ThIfcRoom> spaces)
        {
            double baseX = double.MaxValue;
            var minpoint = Point3d.Origin;
            for (int i = 0; i < spaces.Count; i++)
            {
                
                if ((spaces[i].Boundary.GeometricExtents.MinPoint.X+ spaces[i].Boundary.GeometricExtents.MaxPoint.X)/2 < baseX)
                {
                    baseX = (spaces[i].Boundary.GeometricExtents.MinPoint.X + spaces[i].Boundary.GeometricExtents.MaxPoint.X) / 2;
                }
            }
            return baseX;
        }
    }
}
