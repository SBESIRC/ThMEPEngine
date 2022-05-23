using OfficeOpenXml;
using System;
using System.Data;

namespace ThMEPElectrical.SecurityPlaneSystem.ExcelService
{
    public class ExcelService
    {
        public ExcelService()
        {
            // If you are a commercial business and have
            // purchased commercial licenses use the static property
            // LicenseContext of the ExcelPackage class:
            ExcelPackage.LicenseContext = LicenseContext.Commercial;

            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        public DataSet ReadExcelToDataSet(string fileName, bool isFirstLineColumnName)
        {
            DataSet dataSet = new DataSet();
            using (ExcelPackage package = new ExcelPackage(fileName))
            {
                foreach (ExcelWorksheet sheet in package.Workbook.Worksheets)
                {
                    DataTable dataTable = new DataTable();
                    dataTable.TableName = sheet.Name;
                    int rowCount = sheet.Dimension.Rows;//获取总行数
                    int ColCount = sheet.Dimension.Columns;
                    if (rowCount > 0)
                    {
                        //构建datatable的列
                        for (int i = 1; i <= ColCount; ++i)
                        {
                            string columnName = isFirstLineColumnName ? sheet.Cells[1, i].Value.ToString() : "column" + i;
                            DataColumn column = new DataColumn(columnName);
                            dataTable.Columns.Add(column);
                        }

                        int startRow = isFirstLineColumnName ? 2 : 0;
                        //填充行
                        for (int i = startRow; i <= rowCount; ++i)
                        {
                            if (sheet.Rows[i].Hidden) continue;
                            DataRow dataRow = dataTable.NewRow();
                            for (int j = 1; j <= ColCount; ++j)
                            {
                                dataRow[j-1] = ReadEPPLUSCell(sheet.Cells[i, j]);
                            }
                            dataTable.Rows.Add(dataRow);
                        }
                    }
                    dataSet.Tables.Add(dataTable);
                }
            }
            return dataSet;
        }

        public void ConvertDataSetToExcel(DataSet dataSet, string path)
        {
            using (ExcelPackage pck = new ExcelPackage())
            {
                foreach (DataTable dataTable in dataSet.Tables)
                {
                    ExcelWorksheet workSheet = pck.Workbook.Worksheets.Add(dataTable.TableName);
                    workSheet.Cells["A1"].LoadFromDataTable(dataTable, true);
                }

                pck.SaveAs(path);
            }
        }
        private string ReadEPPLUSCell(ExcelRange cell)
        {
            if (cell.Value == null)
            {
                return "";
            }
            else if (cell.Style.Numberformat.Format.IndexOf("yyyy") > -1 || (cell.Style.Numberformat.Format.IndexOf("月") > -1 &&
                cell.Style.Numberformat.Format.IndexOf("日") > -1))
            {
                return cell.GetValue<DateTime>().ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                return cell.Value.ToString();
            }
        }
    }
}
