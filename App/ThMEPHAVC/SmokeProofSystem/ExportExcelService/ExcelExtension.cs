using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService
{
    public static class ExcelExtension
    {
        public static ExcelWorksheet GetSheetFromSheetName(this ExcelWorkbook workbook, string sheetname)
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

        //public static void SetCellValue(this Worksheet sheet, string cellname, string value)
        //{
        //    Range cell = sheet.Range[cellname];
        //    cell.Value2 = value;
        //}

        public static void CopyRangeToNext(this ExcelWorksheet excelsheet, int fromrow, int fromcolumn, int torow, int tocolumn, int rowoffset)
        {
            var copyedrange = excelsheet.Cells[fromrow, fromcolumn, torow, tocolumn];
            var torange = excelsheet.Cells[fromrow + rowoffset, fromcolumn, torow + rowoffset, tocolumn];
            copyedrange.Copy(torange);

            //Range copyedrange = sheet.Range[rangestartcell, rangeendcell];
            //var newrange = copyedrange.Offset[rowoffset, 0];
            //copyedrange.Copy(newrange);
        }
    }
}
