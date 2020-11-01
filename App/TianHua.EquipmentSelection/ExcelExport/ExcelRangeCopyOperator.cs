using System;
using Microsoft.Office.Interop.Excel;


namespace TianHua.FanSelection.ExcelExport
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

        public void CopyRangeToOtherSheet(Worksheet sourcesheet, string sourcerangestr, Worksheet targetsheet)
        {
            Range sourcerange = sourcesheet.Range[sourcerangestr];
            Range targetrange = targetsheet.Cells[currentrows, currentcolumns];
            targetrange.Insert();
            sourcerange.Copy(targetrange);

            if (currentcolumns < 5)
            {
                currentcolumns += 5;
                lastrowno = Math.Max(lastrowno, currentrows + sourcerange.Rows.Count);
            }
            else
            {
                currentcolumns = 1;
                lastrowno = Math.Max(lastrowno, currentrows + sourcerange.Rows.Count);
                currentrows = lastrowno + 1;
            }
        }

    }
}
