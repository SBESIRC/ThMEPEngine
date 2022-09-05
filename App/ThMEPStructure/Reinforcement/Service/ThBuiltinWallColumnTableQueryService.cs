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
        private readonly string BfKWord = "bf";
        private readonly string HcKWord = "Hc";
        private readonly string Hc1Kword = "Hc1";
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
        public ThEdgeComponent Query(ShapeCode shape, int bwOrhc2, Dictionary<string,int> specDict,
            double stirrupRatio, string antiSeismicGrade,string concreteStrengthGrade)
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
            // 第一步查找 按照specDict和shapeStr查找
            var specArea = FindSpeaArea(sheet, shapeStr, specDict);
            if(specArea.Item1 ==-1)
            {
                return null;
            }
            // 第二步查找 按照sizeKword和bwOrhc2查找
            var bwOrhc2Range = FindSpecRowRange(sheet, specArea.Item1, specArea.Item4 + 1,
                specArea.Item3,sizeKword, bwOrhc2);
            if(bwOrhc2Range.Item1==-1)
            {
                return null;
            }
            var result = FindYBZDatas(sheet, specArea.Item4 + 1, bwOrhc2Range,concreteStrengthGrade, 
                stirrupRatio);
            return ParseYBZ(shape, result);
        }
        public ThEdgeComponent Query(ShapeCode shape, int bwOrhc2, Dictionary<string, int> specDict,
            string position,string antiSeismicGrade)
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
            // 第一步查找 按照specDict和shapeStr查找
            var specArea = FindSpeaArea(sheet, shapeStr, specDict);
            if (specArea.Item1 == -1)
            {
                return null;
            }
            // 第二步查找 按照sizeKword和bwOrhc2查找
            var result = FindGBZDatas(sheet, specArea, bwOrhc2, areaKword);
            return ParseGBZ(shape, result);
        }
        private ThEdgeComponent ParseYBZ(ShapeCode shape, List<Tuple<string, string>> values)
        {
            if (values.Count > 0)
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
            else
            {
                return null;
            }
        }
        private ThEdgeComponent ParseGBZ(ShapeCode shape, List<Tuple<string, string>> values)
        {
            if (values.Count > 0)
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
            else
            {
                return null;
            }
        }
        private ThRectangleEdgeComponent ParseYBZRectType(List<Tuple<string, string>> values)
        {
            var component = new ThRectangleEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            component.StirrUpRatio = GetStirrupRatio(values);
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
            component.StirrUpRatio = GetStirrupRatio(values);
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
            component.StirrUpRatio = GetStirrupRatio(values);
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
            if (IsCloseHoleStirup(values))
            {
                if (linkSpecs.Count == 1)
                {
                    component.Link3 = linkSpecs[0];
                }
                else if (linkSpecs.Count == 2)
                {
                    // 暂时未出现
                }
                else if (linkSpecs.Count == 3)
                {
                    component.Link3 = linkSpecs[1];
                    component.Link4 = linkSpecs[2];
                }
            }
            else
            {
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
            }
            component.Reinforce = GetGBZReinforceSpec(values);
            return component;
        }
        private ThTTypeEdgeComponent ParseGBZTType(List<Tuple<string, string>> values)
        {
            var component = new ThTTypeEdgeComponent();
            component.Stirrup = GetStirrupSpec(values);
            var linkSpecs = GetLinkSpecs(values);
            if(IsCloseHoleStirup(values))
            {
                if (linkSpecs.Count == 1)
                {
                    component.Link3 = linkSpecs[0];
                }
                else if (linkSpecs.Count == 2)
                {
                    // 暂时未出现
                }
                else if (linkSpecs.Count == 3)
                {
                    component.Link3 = linkSpecs[1];
                    component.Link4 = linkSpecs[2];
                }
            }
            else
            {
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
            }
            
            component.Reinforce = GetGBZReinforceSpec(values);
            return component;
        }

        private bool IsCloseHoleStirup(List<Tuple<string, string>> values)
        {
            // 箍筋（临洞口）
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Item1.Contains(StirrupKword) &&
                    values[i].Item1.Contains("临洞口"))
                {
                    return true;
                }
            }
            return false;
        }

        private string ReplaceZToC(string spec)
        {
            return spec.Replace('Z', 'C');
        }
        private string GetYBZReinforceSpec(List<Tuple<string, string>> values)
        {
            var res = values.Where(o => IsContainsShiPeiAs(o.Item2));
            if (res.Count() == 1)
            {
                return GetReinforceSpec(res.First().Item2);
            }
            else
            {
                return "";
            }        
        }

        private bool IsContainsShiPeiAs(string content)
        {
            // 单元格中的内容是否包括"实配AS"
            string pattern = @"(实配){1}\s{0,}(AS)";
            return Regex.IsMatch(content.ToUpper(), pattern);
        }

        private string GetReinforceSpec(string content)
        {
            var values = new List<string>();
            string pattern = @"\d+\s*[ZCzc]{1}\s*\d+";
            foreach(Match match in Regex.Matches(content, pattern))
            {
                values.Add(match.Value.ToUpper());
            }
            if (values.Count > 0)
            {
                var reinforce = string.Join("+", values.ToArray());
                reinforce = RemoveEmpty(reinforce);
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
        private double GetStirrupRatio(List<Tuple<string, string>> values)
        {
            var result = 0.0;
            for (int i = 0; i < values.Count; i++)
            {
                if (IsPvKeyWord(values[i].Item1))
                {
                    var results = GetDoubleValues(values[i].Item2);
                    if(results.Count ==1)
                    {
                        result = results[0];
                    }
                    break;
                }
            }            
            return result;
        }
        private string GetStirrupSpec(List<Tuple<string, string>> values)
        {
            int index = -1;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Item1.Contains(StirrupKword) &&
                    values[i].Item1.Contains("临洞口"))
                {
                    // 对于箍筋(临洞口)
                    index = i;
                    break;
                }
            }
            if(index==-1)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].Item1.Contains(StirrupKword))
                    {
                        index = i;
                        break;
                    }
                }
            }
            if (index != -1)
            {
                return ReplaceZToC(RemoveEmpty(values[index].Item2));
            }
            else
            {
                return "";
            }
        }        
        private Tuple<int, int, int, int> FindSpeaArea(
            ExcelWorksheet worksheet, string shape, Dictionary<string,int> equations)
        {
            var baseColumnIndex = 1; // 第一列
            for (int row = 1; row <= 65536; row++)
            {
                var cellContent = GetCellValue(worksheet, row, baseColumnIndex);
                cellContent = RemoveEmpty(cellContent);
                if (string.IsNullOrEmpty(cellContent))
                {
                     if(IsContinuousRowEmpty(worksheet, row, baseColumnIndex, ContinuousRowCount))
                    {
                        break; // 检查有连续的空格，则退出
                    }
                    else
                    {
                        continue;
                    }
                }                
                var range = GetMergeCell(worksheet, row, baseColumnIndex);
                if(cellContent.ToUpper().Contains(shape) &&
                    IsContains(cellContent,equations))
                {
                    return range;
                }
                else
                {
                    row = range.Item3;
                }
            }
            return Tuple.Create(-1,-1,-1,-1);
        }
        private bool IsContains(string cellContent, Dictionary<string, int> equations)
        {
            if(equations.Count==0)
            {
                return false;
            }
            var newCellContent = cellContent.Replace("，", ",").ToUpper();
            var values = newCellContent.Split(',');

            foreach(var item in equations)
            {
                bool isEqual = false;
                foreach(string value in values)
                {
                    if(IsEqual(value,item.Key,item.Value))
                    {
                        isEqual = true;
                        break;
                    }
                }
                if(isEqual==false)
                {
                    return false;
                }
            }
            return true;
        }
        private bool IsEqual(string equation, string key, int value)
        {
            // Bw=200
            if (IsEquation(equation))
            {
                var strs = equation.Split('=');
                var first = strs[0].Trim().ToUpper();
                var second = int.Parse(strs[1].Trim());
                return first == key.ToUpper() && second == value;
            }
            else
            {
                return false;
            }
        }
        private bool IsEquation(string content)
        {
            // "Bw=100"
            var newContent = content.Trim().ToUpper();
            string pattern = @"^[A-Z0-9]+[\s]*[=]{1}[\s]*\d+$";
            return Regex.IsMatch(newContent, pattern);
        }
        private List<Tuple<string, string>> FindYBZDatas(ExcelWorksheet worksheet, int startColumn,
            Tuple<int, int, int, int> specRows, string concreteStrengthGrade, double stirrupRatio)
        {
            // startColumn->bw,hc2所在的列
            var result = new List<Tuple<string, string>>();
            var specKwordStartRow = specRows.Item1;
            var specKwordEndRow = specRows.Item2;
            var specValueStartRow = specRows.Item3;
            var specValueEndRow = specRows.Item4;
            var keywords = new List<string>();
            for (int row = specValueStartRow; row < specValueEndRow; row++)
            {
                keywords.Add(GetCellValue(worksheet, row, startColumn + 2));
            }
            var columnIndexes = new List<int>();
            for (int column = startColumn + 3; column < 256; column++)
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
                if (cell.ToUpper() == concreteStrengthGrade.ToUpper())
                {
                    columnIndexes.Add(column);
                }
            }
            var datas = new List<List<string>>();
            for (int i = 0; i < columnIndexes.Count; i++)
            {
                var values = new List<string>();
                for (int row = specValueStartRow; row < specValueEndRow; row++)
                {
                    int shiPeiRowIndex = -1,shiPeiColuIndex=-1;
                    // 根据当前行查找实配
                    for (int currentColIndex = columnIndexes[i]; currentColIndex >= 0; currentColIndex--)
                    {
                        var cellString = GetMergeValue(worksheet, row, currentColIndex);
                        if (IsContainsShiPeiAs(cellString))
                        {
                            shiPeiRowIndex = row;
                            shiPeiColuIndex = currentColIndex;
                        }
                    }
                    if(shiPeiRowIndex>=0 && shiPeiColuIndex>=0)
                    {
                        var cellString = GetMergeValue(worksheet, shiPeiRowIndex, shiPeiColuIndex);
                        var reinforceSpec = GetReinforceSpec(cellString);
                        if(!string.IsNullOrEmpty(reinforceSpec))
                        {
                            reinforceSpec = "实配As=" + reinforceSpec;
                            values.Add(reinforceSpec);
                        }
                        else
                        {
                            var cellString1 = GetMergeValue(worksheet, shiPeiRowIndex, shiPeiColuIndex-1);
                            var reinforceSpec1 = GetReinforceSpec(cellString1);
                            if (!string.IsNullOrEmpty(reinforceSpec1))
                            {
                                reinforceSpec1 = "实配As=" + reinforceSpec1;
                                values.Add(reinforceSpec1);
                            }
                        }
                    }
                    else
                    {
                        values.Add(GetMergeValue(worksheet, row, columnIndexes[i]));
                    }  
                }
                datas.Add(values);
            }
            var pminIndex = -1;
            for (int i = 0; i < keywords.Count; i++)
            {
                // 找到此表达式 ρ v(%) 或 ρ v
                if (IsPvKeyWord(keywords[i]))
                {
                    pminIndex = i;
                    break;
                }
            }
            if (pminIndex != -1)
            {
                var dataIndex = -1;
                for (int i = 0; i < datas.Count; i++)
                {
                    var dValues = GetDoubleValues(datas[i][pminIndex]);
                    if (dValues.Count == 1 &&
                        ThReinforcementUtils.IsBiggerThan(dValues[0], stirrupRatio, 2))
                    {
                        dataIndex = i;
                        break;
                    }
                }
                // 如果找不到，就取最后一列
                if(dataIndex == -1)
                {
                    dataIndex = datas.Count - 1;
                }
                if (dataIndex != -1)
                {
                    for (int i = 0; i < keywords.Count; i++)
                    {
                        result.Add(Tuple.Create(keywords[i], datas[dataIndex][i]));
                    }
                }
            }
            return result;
        }
        private List<Tuple<string, string>> FindGBZDatas(ExcelWorksheet worksheet, 
            Tuple<int, int, int, int> specRows,int sizeValue, string areaKword)
        {
            // GBZ->构造件
            // 参数验证放在外面检查
            var result = new List<Tuple<string, string>>();
            var baseColumnIndex = specRows.Item4+1; // Bw和Hc2所在的列
            for (int row = specRows.Item1; row <= specRows.Item3; row++)
            {
                var cellContent = GetCellValue(worksheet, row, baseColumnIndex);
                cellContent = RemoveEmpty(cellContent);
                if(string.IsNullOrEmpty(cellContent))
                {
                    continue;
                }
                var specValue = GetIntegerValue(RemoveEmpty(cellContent));
                if (!specValue.HasValue || specValue.Value != sizeValue)
                {
                    continue;
                }
                var specRange = GetMergeCell(worksheet, row, baseColumnIndex);
                var columnIndexStep = -1;
                if (areaKword == BottomStrengthAreaKword)
                {
                    columnIndexStep = 3;
                }
                else if (areaKword == OtherPartitionKword)
                {
                    columnIndexStep = 4;
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
        private bool IsPvKeyWord(string content)
        {
            //ρ v(%)
            var newContent = content.Trim();
            newContent = newContent.Replace("（", "(");
            newContent = newContent.Replace("）", ")");
            string pattern1 = @"^ρ[\s]*v$";
            string pattern2 = @"^ρ[\s]*v[\s]*[(][\s]*[%][\s]*[)]$";
            return Regex.IsMatch(newContent, pattern1) || Regex.IsMatch(newContent, pattern2);
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
        private List<Tuple<string,string>> GetBelowCellDatas(
            ExcelWorksheet sheel,int startRow, int endRow,int column,int columnStepIndex)
        {
            // column bw or Hc2 所在的列
            var results = new List<Tuple<string, string>>();
            for(int row= startRow; row< endRow;row++)
            {
                var cell1 = GetCellValue(sheel, row, column+2);
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
                var value = sheet.Cells[row, column].Text;
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
            else if(ThReinforcementUtils.GetIntegers(content).Count==1)
            {
                return ThReinforcementUtils.GetIntegers(content)[0];
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
                return "约束" + antiSeismicGrade;
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
            if (!File.Exists(fullPath))
            {
                return;
            }
            var fileInfo = new FileInfo(fullPath);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Package = new ExcelPackage(fileInfo);
        }
    }
}
