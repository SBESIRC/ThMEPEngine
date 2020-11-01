using Microsoft.Office.Interop.Excel;
using System;

namespace TianHua.FanSelection.ExcelExport
{
    public static class ExcelExtension
    {
        public static void SetCellValue(this Worksheet sheet, string cellname, string value)
        {
            Range cell = sheet.Range[cellname];
            cell.Value2 = value;
        }

        public static Worksheet GetSheetFromSheetName(this Workbook workbook, string sheetname)
        {
            try
            {
                return workbook.Worksheets[sheetname];
            }
            catch (System.Exception)
            {
                return null;
            }

        }

        public static void CopyRangeToNext(this Worksheet sheet, string rangestartcell, string rangeendcell, int rowoffset)
        {
            Range copyedrange = sheet.Range[rangestartcell, rangeendcell];
            var newrange = copyedrange.Offset[rowoffset, 0];
            copyedrange.Copy(newrange);
        }
    }
}
