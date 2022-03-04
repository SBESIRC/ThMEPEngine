using System.IO;
using System.Data;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HPSF;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.ExcelService
{
    public class ReadExcelService
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
                workbook = new XSSFWorkbook(fileStream);  //xls数据读入workbook
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
                        var columnNums = new List<int>();//过滤隐藏列
                        for (int i = firstRow.FirstCellNum; i < cellCount; ++i)
                        {
                            if (sheet.IsColumnHidden(i))
                                continue;
                            columnNums.Add(i);
                        }
                        //构建datatable的列
                        columnNums.ForEach(i =>
                        {
                            ICell cell = firstRow.GetCell(i);
                            string columnName = isFirstLineColumnName ? cell.StringCellValue : "column" + (i + 1);
                            DataColumn column = new DataColumn(columnName);
                            dataTable.Columns.Add(column);
                        });

                        int startRow = isFirstLineColumnName ? 1 : 0;
                        //填充行
                        for (int i = startRow; i <= rowCount; ++i)
                        {
                            IRow row = sheet.GetRow(i);
                            if (row == null) continue;
                            if (row.Hidden.Value==true) continue;
                            DataRow dataRow = dataTable.NewRow();
                            for (int j = 0; j < columnNums.Count; j++)
                            {
                                ICell cell = row.GetCell(columnNums[j]);
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

        public void ConvertDataSetToExcel(DataSet dataSet, string path)
        {
            using (MemoryStream ms = DataSetToExcel(dataSet))
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
        }

        /// <summary>
        /// DataSet导出到Excel的MemoryStream
        /// </summary>
        /// <param name="dtSource">源DataSet</param>
        public static MemoryStream DataSetToExcel(DataSet ds)
        {
            XSSFWorkbook workbook = new XSSFWorkbook();
            for (int k = 0; k < ds.Tables.Count; k++)
            {
                //   HSSFSheet sheet = (HSSFSheet)workbook.CreateSheet();
                XSSFSheet sheet = (XSSFSheet)workbook.CreateSheet(ds.Tables[k].TableName.ToString());

                #region 右击文件 属性信息
                {
                    DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                    dsi.Company = "NPOI";

                    SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
                    si.Author = "文件作者信息"; //填加xls文件作者信息
                    si.ApplicationName = "创建程序信息"; //填加xls文件创建程序信息
                    si.LastAuthor = "最后保存者信息"; //填加xls文件最后保存者信息
                    si.Comments = "作者信息"; //填加xls文件作者信息
                    si.Title = "标题信息"; //填加xls文件标题信息
                    si.Subject = "主题信息";//填加文件主题信息
                    si.CreateDateTime = System.DateTime.Now;
                }
                #endregion

                XSSFCellStyle dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                XSSFDataFormat format = (XSSFDataFormat)workbook.CreateDataFormat();
                dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");

                int rowIndex = 0;
                foreach (DataRow row in ds.Tables[k].Rows)
                {
                    #region 新建表，填充表头，填充列头，样式
                    if (rowIndex == 0)
                    {
                        #region 列头及样式
                        {
                            XSSFRow headerRow = (XSSFRow)sheet.CreateRow(0);
                            foreach (DataColumn column in ds.Tables[k].Columns)
                            {
                                headerRow.CreateCell(column.Ordinal).SetCellValue(column.ColumnName);
                            }
                        }
                        #endregion

                        rowIndex = 1;
                    }
                    #endregion

                    #region 填充内容
                    XSSFRow dataRow = (XSSFRow)sheet.CreateRow(rowIndex);
                    foreach (DataColumn column in ds.Tables[k].Columns)
                    {
                        XSSFCell newCell = (XSSFCell)dataRow.CreateCell(column.Ordinal);
                        string drValue = row[column].ToString();
                        switch (column.DataType.ToString())
                        {
                            case "System.String"://字符串类型
                                newCell.SetCellValue(drValue);
                                break;
                            case "System.DateTime"://日期类型
                                System.DateTime dateV;
                                System.DateTime.TryParse(drValue, out dateV);
                                newCell.SetCellValue(dateV);

                                newCell.CellStyle = dateStyle;//格式化显示
                                break;
                            case "System.Boolean"://布尔型
                                bool boolV = false;
                                bool.TryParse(drValue, out boolV);
                                newCell.SetCellValue(boolV);
                                break;
                            case "System.Int16"://整型
                            case "System.Int32":
                            case "System.Int64":
                            case "System.Byte":
                                int intV = 0;
                                int.TryParse(drValue, out intV);
                                newCell.SetCellValue(intV);
                                break;
                            case "System.Decimal"://浮点型
                            case "System.Double":
                                double doubV = 0;
                                double.TryParse(drValue, out doubV);
                                newCell.SetCellValue(doubV);
                                break;
                            case "System.DBNull"://空值处理
                                newCell.SetCellValue("");
                                break;
                            default:
                                newCell.SetCellValue("");
                                break;
                        }

                    }
                    #endregion

                    rowIndex++;
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                ms.Flush();
                workbook.Close();
                return ms;
            }
        }
    }
}
