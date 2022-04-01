using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ThCADExtension;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Service
{
    public class ThBuiltinWallColumnTableQueryService : IDisposable
    {
        private readonly string BwKWord = "bw";
        private readonly string Hc2Kword = "hc2";
        private readonly string ZYBKword = "轴压比";
        private readonly string StirrupKword = "箍筋";
        private readonly string LinkKword = "拉筋";
        private readonly string StirrupRatioKword = "ρ min";
        private readonly string BottomStrengthAreaKword = "底部加强区";
        private readonly string OtherPartitionKword = "其它部位";
        private readonly int ContinuousRowCount = 5;
        private readonly int ContinuousColumnCount = 5;
        private ExcelPackage Package { get; set; }
        public ThBuiltinWallColumnTableQueryService()
        {
            Load();
        }
        public void Dispose()
        {
            if (Package != null)
            {
                Package.Dispose();
            }
        }
        public ThEdgeComponent Query(
            ShapeCode shape,
            int bwOrhc2,
            double stirrupRatio,
            string antiSeismicGrade,
            string concreteStrengthGrade)
        {
            var sheetName = GetSheetName(ComponentType.YBZ, antiSeismicGrade);
            var sheet = GetWorkSheet(sheetName);
            var shapeStr = GetShape(shape);
            var sizeKword = GetSizeKword(shape);
            if (sheet == null || string.IsNullOrEmpty(shapeStr) ||
                string.IsNullOrEmpty(sizeKword) || string.IsNullOrEmpty(concreteStrengthGrade))
            {
                return null;
            }
            var result = Query(sheet, shapeStr, sizeKword, bwOrhc2, concreteStrengthGrade, stirrupRatio);
            return ParseYBZ(shape, result);
        }
        public ThEdgeComponent Query(
            ShapeCode shape,
            int bwOrhc2,
            string position,
            string antiSeismicGrade)
        {
            var sheetName = GetSheetName(ComponentType.GBZ, antiSeismicGrade);
            var sheet = GetWorkSheet(sheetName);
            var shapeStr = GetShape(shape);
            var areaKword = GetAreaKword(position);
            if (sheet == null || string.IsNullOrEmpty(shapeStr) ||
                string.IsNullOrEmpty(areaKword))
            {
                return null;
            }
            var result = Query(sheet, shapeStr,bwOrhc2, areaKword);
            return ParseGBZ(shape, result);
        }
        private ThEdgeComponent ParseYBZ(ShapeCode shape, List<Tuple<string, string>> values)
        {
            switch (shape)
            {
                case ShapeCode.Rect:
                    return ParseYBZRectType(values);
                case ShapeCode.L:
                    return ParseYBZLType(values);
                case ShapeCode.T:
                    return ParseYBZTType(values);
                default:
                    return null;
            }
        }
        private ThEdgeComponent ParseGBZ(ShapeCode shape,List<Tuple<string,string>> values)
        {
            switch (shape)
            {
                case ShapeCode.Rect:
                    return ParseGBZRectType(values);
                case ShapeCode.L:
                    return ParseGBZLType(values);
                case ShapeCode.T:
                    return ParseGBZTType(values);
                default:
                    return null;
            }
        }
        private ThRectangleEdgeComponent ParseYBZRectType(List<Tuple<string, string>> values)
        {
            var component = new ThRectangleEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            var linkSpecs = GetLinkSpecs(values);
            if (linkSpecs.Count == 1)
            {
                component.Link2 = linkSpecs[0];
            }
            else if (linkSpecs.Count == 2)
            {
                component.Link2 = linkSpecs[0];
                component.Link3 = linkSpecs[1];
            }
            component.Reinforce = GetYBZReinforceSpec(values);
            return component;
        }        
        private ThLTypeEdgeComponent ParseYBZLType(List<Tuple<string, string>> values)
        {
            var component = new ThLTypeEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            var linkSpecs = GetLinkSpecs(values);
            if (linkSpecs.Count == 1)
            {
                component.Link2 = linkSpecs[0];
            }
            else if (linkSpecs.Count == 2)
            {
                component.Link2 = linkSpecs[0];
                component.Link3 = linkSpecs[1];
            }
            else if(linkSpecs.Count == 3)
            {
                component.Link2 = linkSpecs[0];
                component.Link3 = linkSpecs[1];
                component.Link4 = linkSpecs[2];
            }
            component.Reinforce = GetYBZReinforceSpec(values);
            return component;
        }
        private ThTTypeEdgeComponent ParseYBZTType(List<Tuple<string, string>> values)
        {
            var component = new ThTTypeEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            var linkSpecs = GetLinkSpecs(values);
            if (linkSpecs.Count == 1)
            {
                component.Link2 = linkSpecs[0];
            }
            else if (linkSpecs.Count == 2)
            {
                component.Link2 = linkSpecs[0];
                component.Link3 = linkSpecs[1];
            }
            else if (linkSpecs.Count == 3)
            {
                component.Link2 = linkSpecs[0];
                component.Link3 = linkSpecs[1];
                component.Link4 = linkSpecs[2];
            }
            component.Reinforce = GetYBZReinforceSpec(values);           
            return component;
        }
        private ThRectangleEdgeComponent ParseGBZRectType(List<Tuple<string, string>> values)
        {
            var component = new ThRectangleEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            var linkSpecs = GetLinkSpecs(values);
            if (linkSpecs.Count == 1)
            {
                component.Link2 = linkSpecs[0];
            }
            else if (linkSpecs.Count == 2)
            {
                component.Link2 = linkSpecs[0];
                component.Link3 = linkSpecs[1];
            }
            component.Reinforce = GetGBZReinforceSpec(values);
            return component;
        }
        private ThLTypeEdgeComponent ParseGBZLType(List<Tuple<string, string>> values)
        {
            var component = new ThLTypeEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            var linkSpecs = GetLinkSpecs(values);
            if (linkSpecs.Count == 1)
            {
                component.Link3 = linkSpecs[0];
            }
            else if (linkSpecs.Count == 2)
            {
                component.Link3 = linkSpecs[0];
                component.Link4 = linkSpecs[1];
            }
            component.Reinforce = GetGBZReinforceSpec(values);
            return component;
        }
        private ThTTypeEdgeComponent ParseGBZTType(List<Tuple<string, string>> values)
        {
            var component = new ThTTypeEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            var linkSpecs = GetLinkSpecs(values);
            if (linkSpecs.Count == 1)
            {
                component.Link3 = linkSpecs[0];
            }
            else if (linkSpecs.Count == 2)
            {
                component.Link3 = linkSpecs[0];
                component.Link4 = linkSpecs[1];
            }
            component.Reinforce = GetGBZReinforceSpec(values);
            return component;
        }

        private string ReplaceZToC(string spec)
        {
            return spec.Replace('Z', 'C');
        }

        private string GetYBZReinforceSpec(List<Tuple<string, string>> values)
        {
            string pattern = @"(实配){1}\s{0,}(AS)\s{0,}[=]{1}";
            var res = values.Where(o => Regex.IsMatch(o.Item2.ToUpper(), pattern));
            if(res.Count()==1)
            {
                return GetReinforceSpec(res.First().Item2);
            }
            else
            {
                return "";
            }
        }
        private string GetReinforceSpec(string content)
        {
            var index = content.IndexOf("实配");
            if(index>0)
            {
                var reinforce = RemoveEmpty(content.Substring(0, index));
                reinforce = ReplaceZToC(reinforce);
                return reinforce;
            }
            else

            {
                return "";
            }
        }
        private string GetGBZReinforceSpec(List<Tuple<string, string>> values)
        {
            var res = values.Where(o => o.Item1.Contains("实配"));
            return res.Count() > 0 ? ReplaceZToC(res.First().Item2) : "";
        }

        private List<string> GetLinkSpecs(List<Tuple<string, string>> values)
        {
            return values
                .Where(o => o.Item1.Contains(LinkKword))
                .Select(o => ReplaceZToC(RemoveEmpty(o.Item2)))
                .ToList();
        }

        private string GetStirrupSpec(List<Tuple<string, string>> values)
        {
            int index = -1;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Item1.Contains(StirrupKword))
                {
                    index = i;
                }
            }
            if(index!=-1)
            {
                return ReplaceZToC(RemoveEmpty(values[index].Item2));
            }
            else
            {
                return "";
            }
        }

        private List<Tuple<string, string>> Query(ExcelWorksheet worksheet, string shape, 
            string sizeKword,int sizeValue, string concreteStrengthGrade, double stirrupRatio)
        {
            // YBZ->约束构件
            // 参数验证放在外面检查
            var result = new List<Tuple<string, string>>();
            var baseColumnIndex = 1; // 第一列
            for (int row = 1; row <= 65536; row++)
            {
                var fourthColunmCell = GetCellValue(worksheet, row, baseColumnIndex + 3);
                if(string.IsNullOrEmpty(fourthColunmCell) &&
                    IsContinuousRowEmpty(worksheet, row, baseColumnIndex + 3, ContinuousRowCount))
                {
                    break; // 检查第四列，若有连续的空格，则退出
                }
                var cellContent = GetCellValue(worksheet, row, baseColumnIndex);
                cellContent = RemoveEmpty(cellContent);
                if(string.IsNullOrEmpty(cellContent))
                {
                    continue;
                }
                var range = GetMergeCell(worksheet, row, baseColumnIndex);
                // 判断外形是否一致
                if (cellContent != shape)
                {
                    row = range.Item3 + 1;
                    continue;
                }
                var specRowRange= FindSpecRowRange(worksheet, row, 
                    baseColumnIndex + 1, range.Item3, sizeKword, sizeValue);
                if(specRowRange.Item1== -1)
                {
                    row = range.Item3 + 1;
                    continue;
                }
                result = FindYBZDatas(worksheet, specRowRange, baseColumnIndex + 1,
                    concreteStrengthGrade, stirrupRatio);
                if(result.Count>0)
                {
                    break;
                }
                else
                {
                    row = range.Item3 + 1;
                    continue;
                }
            }
            return result;
        }
        private List<Tuple<string,string>> FindYBZDatas(ExcelWorksheet worksheet, 
            Tuple<int, int, int, int> specRows,int startColumn,
            string concreteStrengthGrade, double stirrupRatio)
        {
            // startColumn->bw,hc2所在的列
            var result = new List<Tuple<string, string>>();
            var specKwordStartRow = specRows.Item1;
            var specKwordEndRow = specRows.Item2;
            var specValueStartRow = specRows.Item3;
            var specValueEndRow = specRows.Item4;
            var keywords = new List<string>();
            for(int row = specValueStartRow; row < specValueEndRow; row++)
            {
                keywords.Add(GetCellValue(worksheet, row, startColumn + 2));
            }
            var columnIndexes = new List<int>();
            for(int column = startColumn+3; column<256; column++)
            {
                var cell = GetCellValue(worksheet, specKwordEndRow, column);
                cell = RemoveEmpty(cell);
                if (string.IsNullOrEmpty(cell))
                {
                    if (IsContinuousColumnEmpty(worksheet, specKwordEndRow,
                        column, ContinuousColumnCount))
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                if(cell.ToUpper() == concreteStrengthGrade.ToUpper())
                {
                    columnIndexes.Add(column);
                }
            }
            var datas = new List<List<string>>();
            for(int i =0;i< columnIndexes.Count;i++)
            {
                var values = new List<string>();
                for (int row = specValueStartRow; row < specValueEndRow; row++)
                {
                    values.Add(GetMergeValue(worksheet, row, columnIndexes[i]));
                }
                datas.Add(values);
            }
            var pminIndex = -1;
            for(int i =0;i< keywords.Count;i++)
            {
                // ρ min=λ vfc/fy 找到此表达式
                if (keywords[i].Contains("ρ") && keywords[i].ToLower().Contains("min"))
                {
                    pminIndex = i;
                    break;
                }
            }
            if(pminIndex!=-1)
            {
                var dataIndex = -1;
                for(int i=0;i< datas.Count;i++)
                {
                    var dValues = GetDoubleValues(datas[i][pminIndex]);
                    if(dValues.Count ==1 && dValues[0]>= stirrupRatio)
                    {
                        dataIndex = i;
                        break;
                    }                    
                }
                if(dataIndex!=-1)
                {
                    for(int i=0;i<keywords.Count;i++)
                    {
                        result.Add(Tuple.Create(keywords[i], datas[dataIndex][i]));
                    }
                }
            }
            return result;
        }
        private Tuple<int,int,int,int> FindSpecRowRange(ExcelWorksheet sheet,int row,int column,
            int endRow,string sizeKword,int size)
        {
            // 查找规格对应的行
            int specKwordStartRow = -1, specKwordEndRow = -1;
            int specValueStartRow = -1, specValueEndRow = -1;
            for (int i = row;i<= endRow;i++)
            {
                var cell = GetCellValue(sheet,i, column);
                if(!cell.Contains(sizeKword))
                {
                    continue;
                }
                var cellRange = GetMergeCell(sheet, i, column);
                var downCell = GetCellValue(sheet, cellRange.Item3+ 1, column);
                var downCellRange = GetMergeCell(sheet, cellRange.Item3 + 1, column);
                var downCellIntv = GetIntegerValue(downCell);
                if(downCellIntv.HasValue && size == downCellIntv.Value)
                {
                    specKwordStartRow = cellRange.Item1;
                    specKwordEndRow = cellRange.Item3;
                    specValueStartRow = downCellRange.Item1;
                    specValueEndRow = downCellRange.Item3;
                    break;
                }
                else
                {
                    i = downCellRange.Item3;
                }
            }
            return Tuple.Create(specKwordStartRow, specKwordEndRow,
                specValueStartRow, specValueEndRow);
        }
        /// <summary>
        /// 获取单元的合并范围
        /// </summary>
        /// <param name="worksheet">工作集</param>
        /// <param name="row">行号</param>
        /// <param name="column">列号</param>
        /// <returns>起始行号，起始列号，最后行号，最后列号</returns>
        private Tuple<int,int,int,int> GetMergeCell(ExcelWorksheet worksheet,int row,int column)
        {
            var range = worksheet.MergedCells[row, column];
            if(range!=null)
            {
                var address = new ExcelAddress(range);
                return Tuple.Create(address.Start.Row, address.Start.Column, 
                    address.End.Row,address.End.Column);
            }
            else
            {
                return Tuple.Create(row, column,row, column);
            }
        }
        private List<double> GetDoubleValues(string value)
        {
            var results = new List<double>();
            string pattern = @"\d+([.]{1}\d+){0,}";
            foreach(Match match in Regex.Matches(value, pattern))
            {
                results.Add(double.Parse(match.Value));
            }
            return results;
        }
        private List<Tuple<string, string>> Query(ExcelWorksheet worksheet, string shape, 
            int sizeValue, string areaKword)
        {
            // GBZ->构造件
            // 参数验证放在外面检查
            var result = new List<Tuple<string, string>>();
            var baseColumnIndex = 2; // 第二列
            for (int row = 1; row <= 65536; row++)
            {
                var cellContent =GetCellValue(worksheet, row, baseColumnIndex);
                cellContent = RemoveEmpty(cellContent);
                if(string.IsNullOrEmpty(cellContent))
                {
                    if(IsContinuousRowEmpty(worksheet, row, baseColumnIndex, ContinuousRowCount))
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                // 判断外形是否一致
                if(cellContent != shape)
                {
                    continue;
                }
                var specCell = GetCellValue(worksheet, row + 2, baseColumnIndex - 1);
                var specRange = GetMergeCell(worksheet, row + 2, baseColumnIndex - 1);
                if(string.IsNullOrEmpty(specCell))
                {
                    row = specRange.Item3;
                    continue;
                }
                var specValue = GetIntegerValue(RemoveEmpty(specCell));
                if (!specValue.HasValue || specValue.Value != sizeValue)
                {
                    row = specRange.Item3;
                    continue;
                }
                var columnIndexStep = -1;
                if(areaKword == BottomStrengthAreaKword)
                {
                    columnIndexStep = 2;
                }
                else if(areaKword == OtherPartitionKword)
                {
                    columnIndexStep = 3;
                }
                else
                {
                    break;
                }
                result = GetBelowCellDatas(worksheet, specRange.Item1, specRange.Item3, 
                    baseColumnIndex, columnIndexStep);
                break;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sheel"></param>
        /// <param name="startRow">规格合并单元格的起始行</param>
        /// <param name="endRow">规格合并单元格的末尾行</param>
        /// <param name="column">L型、一型、T型，所在列</param>
        /// <param name="columnStepIndex"></param>
        /// <returns></returns>
        private List<Tuple<string,string>> GetBelowCellDatas(
            ExcelWorksheet sheel,int startRow, int endRow,int column,int columnStepIndex)
        {
            var results = new List<Tuple<string, string>>();
            for(int row= startRow; row< endRow;row++)
            {
                var cell1 = GetCellValue(sheel, row, column + 1);
                var cell2 = GetCellValue(sheel, row, column + columnStepIndex);
                results.Add(Tuple.Create(cell1, cell2));
            }
            return results;
        }
        private bool IsContinuousRowEmpty(ExcelWorksheet sheel,int row,int column,int count)
        {
            bool result = true; 
            for(int i= 0;i< count;i++)
            {
                var cellValue = GetCellValue(sheel, row + i, column);
                if(!string.IsNullOrEmpty(cellValue))
                {
                    result = false;
                }
            }
            return result;
        }
        private bool IsContinuousColumnEmpty(ExcelWorksheet sheel, int row, int column, int count)
        {
            bool result = true;
            for (int i = 0; i < count; i++)
            {
                var cellValue = GetCellValue(sheel, row, column+i);
                if (!string.IsNullOrEmpty(cellValue))
                {
                    result = false;
                }
            }
            return result;
        }
        private bool IsContainsBottomStrength(string content)
        {
            var newContent = RemoveEmpty(content);
            return newContent.Contains(BottomStrengthAreaKword);
        }
        private bool IsContainsAsMin(string content)
        {
            var newContent = RemoveEmpty(content).ToLower();
            return newContent.Contains("as") && content.ToLower().Contains("min");
        }
        private string RemoveEmpty(string content)
        {
            return content
                .Replace("\n", "")
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\r", "");
        }
        private string GetCellValue(ExcelWorksheet sheet,int row,int column)
        {
            if(sheet!=null && IsValidRowIndex(row) && IsValidColumnIndex(column))
            {
                var value = sheet.Cells[row, column].Value;
                if(value!=null)
                {
                    return value.ToString();
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        private string GetMergeValue(ExcelWorksheet sheet, int row, int column)
        {
            if(sheet != null && IsValidRowIndex(row) && IsValidColumnIndex(column))
            {
                string range = sheet.MergedCells[row, column];
                if(range == null)
                {
                    return GetCellValue(sheet, row, column);
                }
                else
                {
                    var address = new ExcelAddress(range);
                    return GetCellValue(sheet, address.Start.Row, address.Start.Column);
                }
            }
            else
            {
                return "";
            }
        }

        private bool IsValidColumnIndex(int columnIndex)
        {
            // 256 列
            return columnIndex >= 1 && columnIndex<= 256;
        }
        private bool IsValidRowIndex(int rowIndex)
        {
            // 65536 行
            return rowIndex >= 1 && rowIndex <= 65536;
        }

        private int? GetIntegerValue(string content)
        {
            if(IsInteger(content))
            {
                return int.Parse(content.Trim());
            }
            else
            {
                return null;
            }
        }

        private bool IsInteger(string content)
        {
            var pattern = @"^\s{0,}\d+\s{0,}$";
            return Regex.IsMatch(content, pattern);
        }

        private string GetAreaKword(string position)
        {
            if(position.Contains("底部"))
            {
                return BottomStrengthAreaKword;
            }
            else if (position.Contains("其它") || position.Contains("其他"))
            {
                return OtherPartitionKword;
            }
            else
            {
                return "";
            }
        }
        private string GetSizeKword(ShapeCode shapeCode)
        {
            if(shapeCode == ShapeCode.Rect)
            {
                return BwKWord;
            }
            else if (shapeCode == ShapeCode.L || shapeCode == ShapeCode.T)
            {
                return Hc2Kword;
            }
            else
            {
                return "";
            }
        }

        private string GetShape(ShapeCode shapeCode)
        {
            switch (shapeCode)
            {
                case ShapeCode.Rect:
                    return "一型";
                case ShapeCode.L:
                    return "L型";
                case ShapeCode.T:
                    return "T型";
                default:
                    return "";
            }
        } 

        private ExcelWorksheet GetWorkSheet(string sheetName)
        {
            if(Package==null || string.IsNullOrEmpty(sheetName))
            {
                return null;
            }
            foreach(var sheet in Package.Workbook.Worksheets)
            {
                if(sheet.Name == sheetName)
                {
                    return sheet;
                }
            }
            return null;
        }
        private string GetSheetName(ComponentType componentType, string antiSeismicGrade)
        {
            if(componentType == ComponentType.YBZ)
            {
                if (antiSeismicGrade == "一级" || antiSeismicGrade == "二级")
                {
                    return "约束一二级";
                }
                else
                {
                    return "约束" + antiSeismicGrade;
                }                
            }
            else if(componentType == ComponentType.GBZ)
            {
                return "构造" + antiSeismicGrade;                
            }
            else
            {
                return "";
            }
        }
        private void Load()
        {
            var fullPath = ThCADCommon.TianhuaBuiltInWallColumnTablePath();
            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                return;
            }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Package = new ExcelPackage(fileInfo);
        }
    }
}
