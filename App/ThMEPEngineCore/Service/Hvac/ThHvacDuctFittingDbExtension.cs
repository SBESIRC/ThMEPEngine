using Linq2Acad;
using ThMEPEngineCore.Model.Hvac;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service.Hvac
{
    public static class ThHvacDuctFittingDbExtension
    {
        public static void AddDuctFitting(this Database database, ThIfcDuctFitting ductFitting)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach(Entity entity in ductFitting.Representation)
                {
                    acadDatabase.ModelSpace.Add(entity.GetTransformedCopy(ductFitting.Matrix));
                }
            }
        }
    }
}