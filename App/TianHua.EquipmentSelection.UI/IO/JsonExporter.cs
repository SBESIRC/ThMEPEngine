using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TianHua.FanSelection.UI.IO
{
    /// <summary>
    /// 将DataTable对象，转换成JSON string，并保存到文件中
    /// </summary>
    public class JsonExporter
    {

        private static readonly JsonExporter instance = new JsonExporter();

        public static JsonExporter Instance { get { return instance; } }

        string m_Context = string.Empty;

        public List<DataTable> m_ListTable { get; set; }

        public string _Context
        {
            get
            {
                return m_Context;
            }
        }



        public JsonExporter()
        {

        }

        /// <summary>
        /// 构造函数：完成内部数据创建
        /// </summary>
        /// <param name="_Excel">ExcelLoader Object</param>
        public JsonExporter(ExcelLoader _Excel, bool _ForceSheetName)
        {

            List<DataTable> _ValidSheets = new List<DataTable>();
            for (int i = 0; i < _Excel.Sheets.Count; i++)
            {
                DataTable _Sheet = _Excel.Sheets[i];

                if (_Sheet.Columns.Count > 0 && _Sheet.Rows.Count > 0)
                    _ValidSheets.Add(_Sheet);
            }

            var _JsonSettings = new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd",
                Formatting = Formatting.Indented
            };

            if (!_ForceSheetName && _ValidSheets.Count == 1)
            {

                object _SheetValue = ConvertSheet(_ValidSheets[0], true, false);


                m_Context = JsonConvert.SerializeObject(_SheetValue, _JsonSettings);
            }
            else
            {
                Dictionary<string, object> _Data = new Dictionary<string, object>();
                foreach (var _Sheet in _ValidSheets)
                {
                    object _SheetValue = ConvertSheet(_Sheet, true, false);
                    _Data.Add(_Sheet.TableName, _SheetValue);
                }



                m_Context = JsonConvert.SerializeObject(_Data, _JsonSettings);
            }
        }


        public JsonExporter(ExcelLoader _Excel)
        {
            List<DataTable> _ValidSheets = new List<DataTable>();
            for (int i = 0; i < _Excel.Sheets.Count; i++)
            {
                DataTable _Sheet = _Excel.Sheets[i];

                if (_Sheet.Columns.Count > 0 && _Sheet.Rows.Count > 0)
                    _ValidSheets.Add(_Sheet);
            }
            m_ListTable = _ValidSheets;
        }

        private object ConvertSheet(DataTable _Sheet, bool _ExportArray, bool _Lowcase)
        {
            if (_ExportArray)
                return ConvertSheetToArray(_Sheet, _Lowcase);
            else
                return ConvertSheetToDict(_Sheet, _Lowcase);
        }

        private object ConvertSheetToArray(DataTable _Sheet, bool _Lowcase)
        {
            List<object> _Values = new List<object>();

            int _FirstDataRow = 0;
            for (int i = _FirstDataRow; i < _Sheet.Rows.Count; i++)
            {
                DataRow row = _Sheet.Rows[i];

                _Values.Add(
                    ConvertRowToDict(_Sheet, row, _Lowcase, _FirstDataRow)
                    );
            }

            return _Values;
        }

        /// <summary>
        /// 以第一列为ID，转换成ID->Object的字典对象
        /// </summary>
        private object ConvertSheetToDict(DataTable _Sheet, bool _Lowcase)
        {
            Dictionary<string, object> _ImportData =
                new Dictionary<string, object>();

            int _FirstDataRow = 0;
            for (int i = _FirstDataRow; i < _Sheet.Rows.Count; i++)
            {
                DataRow _Row = _Sheet.Rows[i];
                string ID = _Row[_Sheet.Columns[0]].ToString();
                if (ID.Length <= 0)
                    ID = string.Format("row_{0}", i);

                var _RowObject = ConvertRowToDict(_Sheet, _Row, _Lowcase, _FirstDataRow);
                _ImportData[ID] = _RowObject;
            }

            return _ImportData;
        }

        /// <summary>
        /// 把一行数据转换成一个对象，每一列是一个属性
        /// </summary>
        private Dictionary<string, object> ConvertRowToDict(DataTable _Sheet, DataRow _Row, bool _Lowcase, int _FirstDataRow)
        {
            var _RowData = new Dictionary<string, object>();
            int _Col = 0;
            foreach (DataColumn _Column in _Sheet.Columns)
            {
                object _Value = _Row[_Column];

                if (_Value.GetType() == typeof(System.DBNull))
                {
                    _Value = GetColumnDefault(_Sheet, _Column, _FirstDataRow);
                }
                else if (_Value.GetType() == typeof(double))
                {
                    double _Num = (double)_Value;
                    if ((int)_Num == _Num)
                        _Value = (int)_Num;
                }

                string _FieldName = _Column.ToString();

                if (_Lowcase)
                    _FieldName = _FieldName.ToLower();

                if (string.IsNullOrEmpty(_FieldName))
                    _FieldName = string.Format("col_{0}", _Col);

                _RowData[_FieldName] = _Value;
                _Col++;
            }

            return _RowData;
        }

        /// <summary>
        /// 对于表格中的空值，找到一列中的非空值，并构造一个同类型的默认值
        /// </summary>
        private object GetColumnDefault(DataTable _Sheet, DataColumn _Column, int _FirstDataRow)
        {
            for (int i = _FirstDataRow; i < _Sheet.Rows.Count; i++)
            {
                object _Value = _Sheet.Rows[i][_Column];
                Type _ValueType = _Value.GetType();
                if (_ValueType != typeof(System.DBNull))
                {
                    if (_ValueType.IsValueType)
                        return Activator.CreateInstance(_ValueType);
                    break;
                }
            }
            return "";
        }

        /// <summary>
        /// 将内部数据转换成Json文本，并保存至文件
        /// </summary>
        /// <param name="jsonPath">输出文件路径</param>
        public void SaveToFile(string _FilePath, Encoding _Encoding)
        {
            //-- 保存文件
            using (FileStream _File = new FileStream(_FilePath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter _Writer = new StreamWriter(_File, _Encoding))
                    _Writer.Write(m_Context);
            }
        }


        public void SaveToFile(string _FilePath, Encoding _Encoding, string _Json)
        {
            //-- 保存文件
            using (FileStream _File = new FileStream(_FilePath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter _Writer = new StreamWriter(_File, _Encoding))
                    _Writer.Write(_Json);
            }
        }
    }
}
