using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService
{
    public class ExcelRangeCopyOperator
    {
        public int currentrows { get; set; }
        public int currentcolumns { get; set; }
        public int lastrowno { get; set; }

        public ExcelRangeCopyOperator()
        {
            currentrows = 1;
            currentcolumns = 1;
            lastrowno = 1;
        }

        public void CopyRangeToOtherSheet(ExcelWorksheet sourcesheet, int fromrow, int fromcolumn, int torow, int tocolumn, ExcelWorksheet targetsheet)
        {
            var sourcerange = sourcesheet.Cells[fromrow, fromcolumn, torow, tocolumn];
            var targetposition = targetsheet.Cells[currentrows, currentcolumns];
            sourcerange.Copy(targetposition);

            if (currentcolumns < 5)
            {
                currentcolumns += 5;
                lastrowno = Math.Max(lastrowno, currentrows + sourcerange.Rows);
            }
            else
            {
                currentcolumns = 1;
                lastrowno = Math.Max(lastrowno, currentrows + sourcerange.Rows);
                currentrows = lastrowno + 1;
            }
        }

    }
}
