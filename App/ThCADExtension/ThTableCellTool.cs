using Autodesk.AutoCAD.DatabaseServices;
using System.Text;
namespace ThCADExtension
{
    /// <summary>
    /// CAD表格单元格数据处理
    /// </summary>
    public class ThTableCellTool
    {
        /// 方法借鉴于：https://www.keanw.com/2014/02/exporting-an-autocad-table-to-a-unicode-csv-using-net.html
        /// 上述方法是将CADTable转为CSV数据，下面方法从其中截取一部分,用于对表格单元格数据进行处理
        // Constants used to escape the CSV fields
        private const string QUOTE = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private static char[] MUST_BE_QUOTED = { ',', '"', '\n' };
        public static string Escape(string s)
        {
            if (s.Contains(QUOTE))
                s = s.Replace(QUOTE, ESCAPED_QUOTE);
            if (s.IndexOfAny(MUST_BE_QUOTED) > -1)
                s = QUOTE + s + QUOTE;
            return s;

        }
        // AutoCAD control codes and their Unicode replacements
        // (Codes will be prefixed with "%%")
        private static string[] CODES = { "C", "D", "P" };
        private static string[] REPLS = { "\u00D8", "\u00B0", "\u00B1" };
        public static string ReplaceControlCodes(string s)
        {
            // Check the string for each of our control codes, both
            // upper and lowercase
            for (int i = 0; i < CODES.Length; i++)
            {
                var c = "%%" + CODES[i];
                if (s.Contains(c))
                {
                    s = s.Replace(c, REPLS[i]);
                }
                var c2 = c.ToLower();
                if (s.Contains(c2))
                {
                    s = s.Replace(c2, REPLS[i]);
                }
            }
            return s;
        }
        /// <summary>
        /// 获取表格单元格字符串
        /// 说明：单元格字符串转换后将 ㎡=>m2
        /// </summary>
        /// <param name="tableCell"></param>
        /// <param name="fragAddSpace">
        /// 默认值false
        /// 是否再分割处加入空格（如 (kW/m)  => (kW / m)）
        /// </param>
        /// <param name="escapeResult">
        /// 默认值 false
        /// 是否处理字符串中的引号逗号回车（如果是要存为CSV文件时需要处理） 
        /// 正常使用时使用默认值即可
        /// </param>
        /// <returns></returns>
        public static string TableCellToStringValue(Cell tableCell, bool fragAddSpace = false, bool escapeResult=false) 
        {
            if (null == tableCell)
                return "";
            var sb = FormatStringToStringBuilder(tableCell.GetTextString(FormatOption.ForEditing), fragAddSpace);
            return escapeResult? Escape(sb.ToString()):sb.ToString();
        }
        /// <summary>
        /// 将CAD带格式的字符串去格式化
        /// 说明：单元格字符串转换后将 ㎡=>m2
        /// </summary>
        /// <param name="tableCell"></param>
        /// <param name="fragAddSpace">是否再分割处加入空格（如 (kW/m)  => (kW / m)）默认值false</param>
        /// <returns></returns>
        public static StringBuilder TableCellToStringBuilder(Cell tableCell, bool fragAddSpace = false) 
        {
            var sb = new StringBuilder();
            if (null == tableCell)
                return sb;
            sb = FormatStringToStringBuilder(tableCell.GetTextString(FormatOption.ForEditing), fragAddSpace);
            return sb;
        }
        /// <summary>
        /// 将CAD带格式的字符串去格式化
        /// </summary>
        /// <param name="formatString">CAD带格式字符串</param>
        /// <param name="fragAddSpace">是否再分割处加入空格（如 (kW/m)  => (kW / m)）</param>
        /// <returns></returns>
        public static StringBuilder FormatStringToStringBuilder(string formatString,bool fragAddSpace =false) 
        {
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(formatString))
                return sb;
            using (var mt = new MText())
            {
                mt.Contents = formatString;
                var fragNum = 0;
                mt.ExplodeFragments(
                    (frag, obj) =>
                    {
                        // We'll put spaces between fragments
                        if (fragNum++ > 0 && fragAddSpace)
                        {
                            sb.Append(" ");
                        }
                        // As well as replacing any control codes
                        sb.Append(ReplaceControlCodes(frag.Text));
                        return MTextFragmentCallbackStatus.Continue;
                    }
                );
            }
            return sb;
        }
        /// <summary>
        /// 通过MText获取表格单元格数据，不进行转换
        /// </summary>
        /// <param name="tableCell"></param>
        /// <returns></returns>
        public static string TableCellToTextString(Cell tableCell) 
        {
            var strText = string.Empty;
            if (null == tableCell)
                return strText;
            var cellStr = tableCell.GetTextString(FormatOption.ForEditing);
            using (var mt = new MText())
            {
                mt.Contents = cellStr;
                return mt.Text;
            }
        }
    }
}
