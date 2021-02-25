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
        public List<ThIfcSpace> Spaces { get; set; }
        public ThWTopFloorRecognitionEngine()
        {
            Rooms = new List<ThWTopFloorRoom>();
        }
        public void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWTopFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var blockCollection = new List<BlockReference>();
                blockCollection = BlockTools.GetAllDynBlockReferences(database, "楼层框定");             
                var StandardSpaces = new List<ThIfcSpace>();
                var NonStandardSpaces = new List<ThIfcSpace>();             
                if (blockCollection.Count > 0)
                {                   
                    StandardSpaces = GetStandardSpaces(blockCollection);
                    NonStandardSpaces = GetNonStandardSpaces(blockCollection);
                }        
                var thisSpaces = Spaces;
                var basepoint = GetBaseCircles(blockCollection);                 
                var compositeroom = Getcompositeroom(database, GetBoundaryVertices(StandardSpaces), thisSpaces).Item1;
                var compositebalconyroom = Getcompositeroom(database, GetBoundaryVertices(StandardSpaces), thisSpaces).Item2;
                var divisionLines = GetLines(blockCollection, thisSpaces);
                Rooms = ThTopFloorRoomService.Build(StandardSpaces, basepoint, compositeroom, compositebalconyroom, divisionLines);
            }
        }
        public static Point3dCollection GetBoundaryVertices(List<ThIfcSpace> StandardSpaces)
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
        public static List<ThIfcSpace> GetBaseCircles(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();
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
                            FloorSpaces.Add(new ThIfcSpace { Boundary = baseCircle });
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
        public static List<ThIfcSpace> GetStandardSpaces(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();

            foreach (BlockReference block in blocks)
            {
                var blockBounds = new List<BlockReference>();
                var blockString = new List<string>();
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("标准层"))
                {
                    blockBounds.Add(block);
                }
                blockString.Add(BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号"));
                if (blockBounds.Count > 0)
                {
                    FloorSpaces.Add(new ThIfcSpace { Boundary = GetBoundaryCurves(blockBounds)[0], Tags = blockString });
                }
            }

            return FloorSpaces;
        }
        public static List<ThIfcSpace> GetNonStandardSpaces(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("非标层"))
                {
                    blockBounds.Add(block);
                }
            }
            GetBoundaryCurves(blockBounds).ForEach(o => FloorSpaces.Add(new ThIfcSpace { Boundary = o }));
            return FloorSpaces;
        }
        private Tuple<List<ThWCompositeRoom>, List<ThWCompositeBalconyRoom>>  Getcompositeroom(Database database, Point3dCollection pts,List<ThIfcSpace> spaces)
        {
            using (ThWCompositeRoomRecognitionEngine compositeRoomRecognitionEngine = new ThWCompositeRoomRecognitionEngine())
            {
                compositeRoomRecognitionEngine.Spaces = spaces;
                compositeRoomRecognitionEngine.Recognize(database, pts);
                return Tuple.Create(compositeRoomRecognitionEngine.Rooms, compositeRoomRecognitionEngine.FloorDrainRooms);                  
            }
        }
         private List<Line> GetLines(List<BlockReference> blocks, List<ThIfcSpace> spaces)
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
        private static List<Line> GetColumnLines(List<Line> Columns, List<ThIfcSpace> spaces)
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
        private static double GetMaxPointX(List<ThIfcSpace> spaces)
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
        private static double GetMinPointX(List<ThIfcSpace> spaces)
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
