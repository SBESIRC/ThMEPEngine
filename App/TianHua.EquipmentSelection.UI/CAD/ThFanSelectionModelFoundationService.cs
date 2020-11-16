using Linq2Acad;
using DotNetARX;
using System.Linq;
using Catel.Collections;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionModelFoundationService
    {
        public static void CleanAll(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            using (var manager = new ThFanSelectionDbModelFoundationManager(database))
            {
                manager.Geometries.Cast<ObjectId>().ForEach(o =>
                {
                    acadDatabase.Element<Entity>(o, true).Erase();
                });
            }
        }

        public static void Generate(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            using (var manager = new ThFanSelectionDbModelManager(database))
            {
                manager.Geometries.Cast<ObjectId>().ForEach(o =>
                {
                    var objs = new DBObjectCollection();
                    var foundation = new ObjectIdCollection();
                    var model = acadDatabase.Element<BlockReference>(o);
                    model.Explode(objs);
                    objs.Cast<Entity>()
                    .Where(e => e.IsModelFoundationLayer())
                    .ForEach(f =>
                    {
                        foundation.Add(acadDatabase.ModelSpace.Add(f));
                    });
                    foundation.Cast<ObjectId>().ForEach(f =>
                    {
                        TypedValueList valueList = new TypedValueList
                        {
                            { (int)DxfCode.ExtendedDataAsciiString, model.GetModelIdentifier() },
                        };
                        f.AddXData(ThFanSelectionCommon.RegAppName_Model_Foundation, valueList);
                    });
                });
            }
        }
    }
}
