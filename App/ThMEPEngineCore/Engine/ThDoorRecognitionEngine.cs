using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDoorRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        private double FindRatio { get; set; } = 1.0;
        public ThDoorRecognitionEngine(double findRaio)
        {
            FindRatio = findRaio;
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))            
            {
                var doorStones = GetDoorStones(database, polygon);
                var doorMarks = GetDoorMarks(database, polygon);
                var outlines = ThMatchDoorStoneService.Match(doorStones, doorMarks, FindRatio);
                outlines.ForEach(o => Elements.Add(new ThIfcDoor() { Outline = o }));
            }
        }
        private DBObjectCollection GetDoorStones(Database database, Point3dCollection polygon)
        {
            using (var doorStoneDbExtension = new ThDoorStoneDbExtension(database))
            {
                doorStoneDbExtension.BuildElementCurves();
                var stones = new DBObjectCollection();
                if (polygon.Count > 0)
                {
                    var dbObjs = new DBObjectCollection();
                    doorStoneDbExtension.Stones.ForEach(o => dbObjs.Add(o));
                    var doorStoneSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in doorStoneSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        stones.Add(filterObj as Curve);
                    }
                }
                else
                {
                    stones = doorStoneDbExtension.Stones.ToCollection();
                }
                return stones;
            }
        }
        private DBObjectCollection GetDoorMarks(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var doorMarkDbExtension = new ThDoorMarkDbExtension(database))
            {
                doorMarkDbExtension.BuildElementTexts();
                var marks = new DBObjectCollection();
                if (polygon.Count > 0)
                {
                    var dbObjs = new DBObjectCollection();
                    doorMarkDbExtension.Texts.ForEach(o => dbObjs.Add(o));
                    var doorMarkSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in doorMarkSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        marks.Add(filterObj as Entity);
                    }
                }
                else
                {
                    marks = doorMarkDbExtension.Texts.ToCollection();
                }
                return marks;
            }
        }
    }
}
