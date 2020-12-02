using Linq2Acad;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertTableUtils
    {
        public static string Text(this Cell cell)
        {
            if (cell.Value == null)
            {
                return string.Empty;
            }
            using (var mText = new MText()
            {
                Contents = cell.Value.ToString(),
            })
            {
                return Regex.Replace(mText.Text, @"\t|\n|\r", "");
            }
        }

        public static string BlockName(this Cell cell)
        {
            var blockId = cell.BlockTableRecordId;
            if (blockId.IsNull)
            {
                return string.Empty;
            }
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockId.Database))
            {
                return acadDatabase.Element<BlockTableRecord>(blockId).Name;
            }
        }
    }
}
