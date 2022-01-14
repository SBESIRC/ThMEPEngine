using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.IO;

namespace ThMEPEngineCore.IO.ExcelService
{
    public abstract class NPOIExcelBase
    {
        public IWorkbook ReadExcelToMemory(string filePath,bool writeFile =false)
        {
            IWorkbook workbook = null;  //新建IWorkbook对象
            FileStream fileStream = null;
            try
            {
                //这里不检查文件是否存在
                var fileExt = Path.GetExtension(filePath);
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                if (fileExt.ToLower() == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fileStream);  //xlsx数据读入workbook
                }
                else if (fileExt.ToLower() == ".xls")
                {
                    workbook = new HSSFWorkbook(fileStream);  //xls数据读入workbook
                }
                else
                {
                    throw new Exception("格式不支持");
                }
                return workbook;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //防止解析报错内存资源没有释放,如果外面要写入Excel,这里将资源释放，会报错，无法写入Excel
                if (workbook != null && !writeFile)
                    workbook.Close();
                if (null != fileStream)
                    fileStream.Close();
            }
        }
        /// <summary>
        /// 获取单元格类型
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public object GetValueType(ICell cell)
        {
            if (cell == null)
                return null;
            
            switch (cell.CellType)
            {
                case CellType.Blank: //BLANK:  
                    return null;
                case CellType.Boolean: //BOOLEAN:  
                    return cell.BooleanCellValue;
                case CellType.Numeric: //NUMERIC:  
                    short shortFormat = cell.CellStyle.DataFormat;
                    //对时间格式（2015.12.5、2015/12/5、2015-12-5等）的处理
                    if (shortFormat == 14 || shortFormat == 31 || shortFormat == 57 || shortFormat == 58)
                        return cell.DateCellValue;
                    else
                    {
                        return cell.NumericCellValue;
                    }
                case CellType.String: //STRING:  
                    return cell.StringCellValue;
                case CellType.Error: //ERROR:  
                    return cell.ErrorCellValue;
                case CellType.Formula: //FORMULA:
                    //针对公式列 进行动态计算;注意：公式暂时只支持 数值 字符串类型
                    var type = cell.CachedFormulaResultType;
                    if (type == CellType.Numeric)
                    {
                        var value = cell.NumericCellValue.ToString();
                        var formatStr = cell.CellStyle.GetDataFormatString();
                        if (formatStr.Contains("_"))
                            formatStr = formatStr.Substring(0, formatStr.IndexOf("_"));
                        if (!string.IsNullOrEmpty(value)  && !string.IsNullOrEmpty(formatStr)) 
                        {
                            value = cell.NumericCellValue.ToString(formatStr);
                            var dValue = 0.0;
                            double.TryParse(value,out dValue);
                            return dValue;
                        }
                        return cell.NumericCellValue;
                    }
                    else if (type == CellType.String)
                    {
                        return cell.StringCellValue;
                    }
                    return cell.StringCellValue;
                default:
                    return "=" + cell.CellFormula;
            }
        }
    }
}
