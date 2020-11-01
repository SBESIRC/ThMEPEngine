using System;
using System.IO;
using System.Data;
using ExcelDataReader;

namespace TianHua.FanSelection.UI.IO
{

    public class ExcelLoader
    {
        private DataSet m_Data;

        public ExcelLoader(string _FilePath, int _HeaderRow)
        {
            using (var _Stream = File.Open(_FilePath, FileMode.Open, FileAccess.Read))
            {
                using (var _Reader = ExcelReaderFactory.CreateReader(_Stream))
                {
                    var _Result = _Reader.AsDataSet(CreateDataSetReadConfig(_HeaderRow));
                    this.m_Data = _Result;
                }
            }

            if (this.Sheets.Count < 1)
            {
                throw new Exception("Excel file is empty: " + _FilePath);
            }
        }

        public DataTableCollection Sheets
        {
            get
            {
                return this.m_Data.Tables;
            }
        }

        private ExcelDataSetConfiguration CreateDataSetReadConfig(int _HeaderRow)
        {
            var _TableConfig = new ExcelDataTableConfiguration()
            {
                UseHeaderRow = true,
                FilterRow = (_RowReader) =>
                {
                    return _RowReader.Depth > _HeaderRow - 1;
                },
            };

            return new ExcelDataSetConfiguration()
            {
                UseColumnDataType = true,
                ConfigureDataTable = (_TableReader) => { return _TableConfig; },
            };
        }
    }
}
