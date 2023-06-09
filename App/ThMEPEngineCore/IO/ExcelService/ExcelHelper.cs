﻿using NPOI.SS.UserModel;
using System;
using System.Data;
using System.IO;

namespace ThMEPEngineCore.IO.ExcelService
{
    public class ExcelHelper: NPOIExcelBase
    {
        public DataSet ReadExcelToDataSet(string filePath, int startRow=0)
        {
            IWorkbook workbook = null;
            try
            {
                workbook = ReadExcelToMemory(filePath);
                var dataSet = new DataSet();
                if (null == workbook)
                    return dataSet;
                dataSet = ReadExcelToDataSet(workbook, startRow);
                return dataSet;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally 
            {
                //防止解析报错内存资源没有释放
                if(null != workbook)
                    workbook.Close();
            }
        }
        public DataSet ReadExcelToDataSet(IWorkbook workbook,int startRow)
        {
            var dataSet = new DataSet();
            if (null == workbook)
                return dataSet;
            for (int p = 0; p < workbook.NumberOfSheets; p++)
            {
                var sheet = workbook.GetSheetAt(p);  //获取第一个工作表
                var dataTable = ReadDataFromIWorkBook(sheet, startRow);
                if (null == dataTable)
                    continue;
                dataSet.Tables.Add(dataTable);
            }
            return dataSet;
        }
        public DataTable ReadDataFromIWorkBook(ISheet sheet,int startRow) 
        {
            var dataTable = new DataTable();
            if (sheet == null)
                return dataTable;
            dataTable.TableName = sheet.SheetName;
            int rowCount = sheet.LastRowNum;//获取总行数
            if (rowCount < 1)
                return dataTable;
            var columnCount = 1;
            var startCell = int.MaxValue;
            for (int i = startRow; i < rowCount; ++i) 
            {
                var thisRow = sheet.GetRow(i);
                var thisEnd = thisRow.LastCellNum;//获取总列数
                var thisFirst = thisRow.FirstCellNum;
                columnCount = Math.Max(columnCount, thisEnd);
                startCell = Math.Min(startCell, thisFirst);
            }
            //构建datatable的列
            for (int i = startCell; i < columnCount; ++i)
            {
                DataColumn column = new DataColumn();
                dataTable.Columns.Add(column);
            }
            
            //填充行
            for (int i = startRow; i < rowCount; ++i)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;
                if (row.Hidden.Value == true) continue;
                var dataRow = dataTable.NewRow();
                for (int j = row.FirstCellNum; j < columnCount; ++j)
                {
                    ICell cell = row.GetCell(j);
                    int dataRowIndex = j - row.FirstCellNum;
                    if (cell == null)
                    {
                        dataRow[dataRowIndex] = "";
                    }
                    else
                    {
                        dataRow[dataRowIndex] = GetValueType(cell);
                    }
                }
                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }
        public IWorkbook ReadCopyExcelToMemory(string filePath) 
        {
            //为了防止Excel在打开中，这里读取报错，这里将文件复制的一份，读取结束后将复制的文件删除
            string tempPath = "";
            try
            {
                string dirPath = Path.GetDirectoryName(filePath);
                string fileExt = Path.GetExtension(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                string tmpFile = string.Format("~{0}{1}", fileName, fileExt);
                tempPath = Path.Combine(dirPath, tmpFile);
                File.Copy(filePath, tempPath, true);
                File.SetAttributes(tempPath, FileAttributes.Normal);
                return ReadExcelToMemory(tempPath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally 
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.SetAttributes(tempPath, FileAttributes.Normal);
                        File.Delete(tempPath);
                    }
                    catch{ }
                }
            }
        }
    }
}
