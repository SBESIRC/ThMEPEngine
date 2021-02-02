using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWTopFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWTopFloorRoom> Rooms { get; set; }
        public ThWTopFloorRecognitionEngine()
        {
            Rooms = new List<ThWTopFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWTopFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                var basepoint = new List<ThIfcSpace>();
                var compositeroom = Getcompositeroom(database, pts);
                var compositebalconyroom = Getcompositebalconyroom(database, pts);
                var divisionLines = GetLines(database, this.Spaces);
                Rooms = ThTopFloorRoomService.Build(this.Spaces, basepoint, compositeroom, compositebalconyroom, divisionLines);
            }
        }
        private List<ThWCompositeRoom> Getcompositeroom(Database database, Point3dCollection pts)
        {
            using (ThWCompositeRoomRecognitionEngine compositeRoomRecognitionEngine = new ThWCompositeRoomRecognitionEngine())
            {
                compositeRoomRecognitionEngine.Recognize(database, pts);
                return compositeRoomRecognitionEngine.Rooms;
            }
        }
        private List<ThWCompositeBalconyRoom> Getcompositebalconyroom(Database database, Point3dCollection pts)
        {
            using (ThWCompositeRoomRecognitionEngine compositeRoomRecognitionEngine = new ThWCompositeRoomRecognitionEngine())
            {
                compositeRoomRecognitionEngine.Recognize(database, pts);
                return compositeRoomRecognitionEngine.FloorDrainRooms;
            }
        }
         private List<Line> GetLines(Database database,List<ThIfcSpace> spaces)
        {
            var Columns = new List<Line>();         
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var divisionLinesDbExtension = new ThDivisionLinesDbExtension(database))
            {
                divisionLinesDbExtension.BuildElementCurves();
                Columns = divisionLinesDbExtension.Lines;
            }
            if (Columns.Count > 0)
            {

                return GetColumnLines(Columns, spaces);
            }
            else
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
                {
                    var lines = acadDatabase.ModelSpace.OfType<Line>().Where(lineInfo => lineInfo.Layer.Contains(ThWPipeCommon.AD_FLOOR_AREA)).ToList();
                    return lines;
                }
            }
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
