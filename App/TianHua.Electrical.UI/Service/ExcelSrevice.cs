using System;
using System.Data;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TianHua.Electrical.UI.Service
{
    public class ExcelSrevice
    {
        public DataSet ReadExcelToDataSet(string fileName, bool isFirstLineColumnName)
        {
            IWorkbook workbook = null;  //新建IWorkbook对象
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            if (fileName.IndexOf(".xlsx") > 0)
            {
                workbook = new XSSFWorkbook(fileStream);  //xlsx数据读入workbook
            }
            else if (fileName.IndexOf(".xls") > 0)
            {
                workbook = new HSSFWorkbook(fileStream);  //xls数据读入workbook
            }

            DataSet dataSet = new DataSet();
            if (workbook != null)
            {
                for (int p = 0; p < workbook.NumberOfSheets; p++)
                {
                    ISheet sheet = workbook.GetSheetAt(p);  //获取第一个工作表
                    DataTable dataTable = new DataTable();
                    dataTable.TableName = sheet.SheetName;

                    int rowCount = sheet.LastRowNum;//获取总行数
                    if (rowCount > 0)
                    {
                        IRow firstRow = sheet.GetRow(0);//获取第一行
                        int cellCount = firstRow.LastCellNum;//获取总列数

                        //构建datatable的列
                        for (int i = firstRow.FirstCellNum; i < cellCount; ++i)
                        {
                            ICell cell = firstRow.GetCell(i);
                            string columnName = isFirstLineColumnName ? cell.StringCellValue : "column" + (i + 1);
                            DataColumn column = new DataColumn(columnName);
                            dataTable.Columns.Add(column);
                        }

                        int startRow = isFirstLineColumnName ? 1 : 0;
                        //填充行
                        for (int i = startRow; i <= rowCount; ++i)
                        {
                            IRow row = sheet.GetRow(i);
                            if (row == null) continue;

                            DataRow dataRow = dataTable.NewRow();
                            for (int j = row.FirstCellNum; j < cellCount; ++j)
                            {
                                ICell cell = row.GetCell(j);
                                if (cell == null)
                                {
                                    dataRow[j] = "";
                                }
                                else
                                {
                                    //CellType(Unknown = -1,Numeric = 0,String = 1,Formula = 2,Blank = 3,Boolean = 4,Error = 5,)
                                    switch (cell.CellType)
                                    {
                                        case CellType.Blank:
                                            dataRow[j] = "";
                                            break;
                                        case CellType.Numeric:
                                            short format = cell.CellStyle.DataFormat;
                                            //对时间格式（2015.12.5、2015/12/5、2015-12-5等）的处理
                                            if (format == 14 || format == 31 || format == 57 || format == 58)
                                                dataRow[j] = cell.DateCellValue;
                                            else
                                                dataRow[j] = cell.NumericCellValue;
                                            break;
                                        case CellType.String:
                                            dataRow[j] = cell.StringCellValue;
                                            break;
                                    }
                                }
                            }
                            dataTable.Rows.Add(dataRow);
                        }
                    }

                    dataSet.Tables.Add(dataTable);
                }
            }

            workbook.Close();
            fileStream.Close();
            return dataSet;
        }
    }
}
