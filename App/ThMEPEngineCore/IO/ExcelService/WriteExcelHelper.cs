using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ThMEPEngineCore.IO.ExcelService
{
    public class WriteExcelHelper : NPOIExcelBase
    {
        string saveExcelPath = "";
        public WriteExcelHelper(string excelPath)
        {
            saveExcelPath = excelPath;
        }
        public void CopySheetFromIndex(List<string> sheetNames,int copyFromIndex) 
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(saveExcelPath, true);
                int count = sheetNames.Count;
                for (int i = 0; i < count; i++) 
                {
                    var addSheetName = sheetNames[i];
                    var addSheet = workbook.CloneSheet(copyFromIndex);
                    var getIndex = workbook.GetSheetIndex(addSheet);
                    workbook.SetSheetName(getIndex, addSheetName);
                }
                using (FileStream filess = new FileStream(saveExcelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(filess);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //防止解析报错内存资源没有释放
                if (null != workbook)
                    workbook.Close();
            }
        }
        public void AddSheets(List<string> sheetNames)
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(saveExcelPath, true);
                int count = sheetNames.Count;
                for (int i = 0; i < count; i++)
                {
                    var addSheetName = sheetNames[i];
                    var addSheet = workbook.CreateSheet(addSheetName);
                }
                using (FileStream filess = new FileStream(saveExcelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(filess);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //防止解析报错内存资源没有释放
                if (null != workbook)
                    workbook.Close();
            }
        }
        public void DeleteExcelSheet(List<string> sheetNames,bool isDelete) 
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(saveExcelPath,true);
                var delSheetIndex = new List<int>();
                for (int p = 0; p < workbook.NumberOfSheets; p++)
                {
                    var sheet = workbook.GetSheetAt(p);
                    var sheetName = sheet.SheetName;
                    bool inNames = sheetNames.Any(c => c == sheetName);
                    if (inNames == isDelete)
                        delSheetIndex.Add(p);
                }
                //删除sheet后，其它sheet会跟着进行移动位置，这里从位置最大的开始删除
                delSheetIndex = delSheetIndex.OrderByDescending(c => c).ToList();
                for (int i = 0; i < delSheetIndex.Count; i++)
                {
                    workbook.RemoveSheetAt(delSheetIndex[i]);
                }
                using (FileStream filess = new FileStream(saveExcelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(filess);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //防止解析报错内存资源没有释放
                if (null != workbook)
                    workbook.Close();
            }
        }
        public void DeleteExcelSheet(List<int> deleteSheetIndexs)
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(saveExcelPath, true);

                //删除sheet后，其它sheet会跟着进行移动位置，这里从位置最大的开始删除
                deleteSheetIndexs = deleteSheetIndexs.OrderByDescending(c => c).ToList();
                for (int i = 0; i < deleteSheetIndexs.Count; i++)
                {
                    var sheet = workbook.GetSheetAt(deleteSheetIndexs[i]);
                    if (null == sheet)
                        continue;
                    workbook.RemoveSheetAt(deleteSheetIndexs[i]);
                }
                using (FileStream filess = new FileStream(saveExcelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(filess);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //防止解析报错内存资源没有释放
                if (null != workbook)
                    workbook.Close();
            }
        }
        public void DeleteExcelRow(string sheetName, int startDelRowIndex, int endDexRowIndex) 
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(saveExcelPath, true);
                ISheet writeSheet = workbook.GetSheet(sheetName);
                DeleteExcelSheetRows(writeSheet,startDelRowIndex,endDexRowIndex);
                using (FileStream filess = new FileStream(saveExcelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(filess);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //防止解析报错内存资源没有释放
                if (null != workbook)
                    workbook.Close();
            }
        }
        public void DeleteExcelRow(int sheetIndex, int startDelRowIndex, int endDexRowIndex)
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(saveExcelPath, true);
                ISheet writeSheet = workbook.GetSheetAt(sheetIndex);
                DeleteExcelSheetRows(writeSheet, startDelRowIndex, endDexRowIndex);
                using (FileStream filess = new FileStream(saveExcelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(filess);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //防止解析报错内存资源没有释放
                if (null != workbook)
                    workbook.Close();
            }
        }
        public void WriteDataTableToTemplateExcel(DataTable dataTable,string sheetName, int startRow=0)
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(saveExcelPath,true);
                ISheet ws = string.IsNullOrEmpty(sheetName)? workbook.GetSheetAt(0):workbook.GetSheet(sheetName);
                if (ws == null)
                {
                    //工作薄中没有工作表
                    throw new Exception(string.Format("工作薄中没有‘{0}’工作表", sheetName));
                }
                XSSFCellStyle dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                XSSFDataFormat format = (XSSFDataFormat)workbook.CreateDataFormat();
                dateStyle.DataFormat = format.GetFormat("yyyy-mm-dd");
                int rowCount = dataTable.Rows.Count;
                int columnCount = dataTable.Columns.Count;
                if (rowCount > 0)
                {
                    for (int i = startRow; i < rowCount+ startRow; i++)
                    {
                        int _row = i;
                        var row = ws.GetRow(i);
                        if(null ==row)
                            row = ws.CreateRow(_row);
                        var dataRow = dataTable.Rows[i - startRow];
                        for (int j = 0; j < columnCount; j++) 
                        {
                            var newCell = row.GetCell(j);
                            if(newCell ==null)
                                newCell = row.CreateCell(j);
                            var dataType = dataTable.Rows[i - startRow][j].GetType().FullName;
                            string drValue = dataTable.Rows[i - startRow][j].ToString();
                            switch (dataType.ToString())
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
                    }
                }
                ws.ForceFormulaRecalculation = true;
                using (FileStream filess = new FileStream(saveExcelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(filess);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally 
            {
                if (null != workbook)
                    workbook.Close();
            }
        }
        private void DeleteExcelSheetRows(ISheet writeSheet,int startDelRowIndex,int endDexRowIndex) 
        {
            if (null == writeSheet)
                return;
            int rowCount = writeSheet.LastRowNum;//获取总行数
            var endRow = endDexRowIndex < 0 ? rowCount : endDexRowIndex;
            //删除行后面的行号会改变,从最大的行开始删除
            for (int i = endRow; i>startDelRowIndex; i--) 
            {
                if (i < 0)
                    break;
                var row = writeSheet.GetRow(i);
                if (row == null)
                    continue;
                writeSheet.RemoveRow(row);
            }
        }
    }
}
