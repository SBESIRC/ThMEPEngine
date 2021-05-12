using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore
{
    public static class ThMEPEngineCoreLayerUtils
    {
        public static ObjectId CreateAILayer(this Database database, string name, short colorIndex)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = database.AddLayer(name);
                database.UnOffLayer(name);
                database.UnLockLayer(name);
                database.UnPrintLayer(name);
                database.UnFrozenLayer(name);
                database.SetLayerColor(name, colorIndex);
                return layerId;
            }
        }
    }
}
