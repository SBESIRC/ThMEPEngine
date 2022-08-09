using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore
{
    public static class ThMEPEngineCoreTextStyleUtils
    {
        public static ObjectId ImportTextStyle(this Database database, string textStyle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.StoreyFrameDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                return acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(textStyle), false).Item.ObjectId;
            }
        }
    }
}
