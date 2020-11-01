using System;
using System.IO;
using Microsoft.Office.Interop.Excel;

namespace TianHua.FanSelection.ExcelExport
{
    public class ExcelFile
    {
        public Application ExcelApp { get; set; }

        public ExcelFile()
        {
            ExcelApp = new Application
            {
                DisplayAlerts = false,
                Visible = false,
                ScreenUpdating = false
            };
        }

        public void Close()
        {
            ExcelApp.Quit();
        }

        public Workbook OpenWorkBook(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            return ExcelApp.Workbooks.Open(path, System.Type.Missing, System.Type.Missing, System.Type.Missing,
              System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing,
            System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);
        }

        public void SaveWorkbook(Workbook workbook, string savepath)
        {
            workbook.SaveAs(savepath, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSaveAsAccessMode.xlNoChange,
                   Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
        }

    }
}
