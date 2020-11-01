using System.IO;
using System.Linq;
using System.Text;
using System.Data;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;

namespace TianHua.FanSelection.UI.IO
{
    public class DataManager
    {
        private Encoding m_Encoding;

        public JsonExporter m_Json;


        /// <summary>
        /// 离心-前倾-单速
        /// </summary>
        public List<FanParameters> m_ListFan_Forerake_Single = new List<FanParameters>();

        /// <summary>
        /// 离心-前倾-双速
        /// </summary>
        public List<FanParameters> m_ListFan_Forerake_Double = new List<FanParameters>();

        /// <summary>
        /// 离心-后倾-单速
        /// </summary>
        public List<FanParameters> m_ListFan_Hypsokinesis_Single = new List<FanParameters>();


        /// <summary>
        /// 轴流-单速
        /// </summary>
        public List<AxialFanParameters> m_ListAxialFan_Single = new List<AxialFanParameters>();
        /// <summary>
        /// 轴流-双速
        /// </summary>
        public List<AxialFanParameters> m_ListAxialFan_Double = new List<AxialFanParameters>();


        public string JsonContext
        {
            get
            {
                if (m_Json != null)
                    return m_Json._Context;
                else
                    return "";
            }
        }

        public void SaveJson(string _FilePath)
        {
            if (m_Json != null)
            {
                m_Json.SaveToFile(_FilePath, m_Encoding);
            }
        }

        public void LoadExcel(string _Path)
        {
            string _ExcelPath = _Path;

            string _ExcelName = Path.GetFileNameWithoutExtension(_ExcelPath);

            int _Header = 0;

            Encoding _Encoding = new UTF8Encoding(false);

            m_Encoding = _Encoding;

            ExcelLoader _Excel = new ExcelLoader(_ExcelPath, _Header);

            m_Json = new JsonExporter(_Excel);

            m_ListFan_Forerake_Single = new List<FanParameters>();

            m_ListFan_Forerake_Double = new List<FanParameters>();

            m_ListFan_Hypsokinesis_Single = new List<FanParameters>();

            m_ListAxialFan_Single = new List<AxialFanParameters>();

            m_ListAxialFan_Double = new List<AxialFanParameters>();

            SetBasicData();
        }

        public void ExportExcl(string _Path, string _ExportType)
        {
            string _ExcelPath = _Path;

            string _ExcelName = Path.GetFileNameWithoutExtension(_ExcelPath);

            int _Header = 0;

            ExcelLoader _Excel = new ExcelLoader(_ExcelPath, _Header);

        }

        public void SetBasicData()
        {
            if (m_Json.m_ListTable == null || m_Json.m_ListTable.Count == 0) { return; }
            for (int i = 0; i < m_Json.m_ListTable.Count; i++)
            {
                if (FuncStr.NullToStr(m_Json.m_ListTable[i].TableName) == "离心-前倾-单速")
                {
                    m_ListFan_Forerake_Single = InitFanParameters(m_Json.m_ListTable[i]);
                }
                if (FuncStr.NullToStr(m_Json.m_ListTable[i].TableName) == "离心-前倾-双速")
                {
                    m_ListFan_Forerake_Double = InitFanParameters(m_Json.m_ListTable[i]);
                }
                if (FuncStr.NullToStr(m_Json.m_ListTable[i].TableName) == "离心-后倾-单速")
                {
                    m_ListFan_Hypsokinesis_Single = InitFanParameters(m_Json.m_ListTable[i]);
                }
                if (FuncStr.NullToStr(m_Json.m_ListTable[i].TableName) == "轴流-单速")
                {
                    m_ListAxialFan_Single = InitAxialFanParameters(m_Json.m_ListTable[i]);
                }
                if (FuncStr.NullToStr(m_Json.m_ListTable[i].TableName) == "轴流-双速")
                {
                    m_ListAxialFan_Double = InitAxialFanParameters(m_Json.m_ListTable[i]);
                }
            }
        }

        private List<AxialFanParameters> InitAxialFanParameters(DataTable _DataTable)
        {
            var _List = new List<AxialFanParameters>();
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                AxialFanParameters _AxialFanParameters = new AxialFanParameters()
                {
                    No = FuncStr.NullToStr(_DataTable.Rows[i]["机号"]),
                    ModelNum = FuncStr.NullToStr(_DataTable.Rows[i]["型号"]),
                    Rpm = FuncStr.NullToStr(_DataTable.Rows[i]["转速"]),
                    AirVolume = FuncStr.NullToStr(_DataTable.Rows[i]["风量m^3/h"]),
                    Pa = FuncStr.NullToStr(_DataTable.Rows[i]["全压(Pa)"]),
                    Power = FuncStr.NullToStr(_DataTable.Rows[i]["功率(Kw)"]),
                    Noise = FuncStr.NullToStr(_DataTable.Rows[i]["噪声dB(A)"]),
                    Weight = FuncStr.NullToStr(_DataTable.Rows[i]["重量(Kg)"]),
                    Diameter = FuncStr.NullToStr(_DataTable.Rows[i]["直径"]),
                    Length = FuncStr.NullToStr(_DataTable.Rows[i]["长度"])
                };

                if (_DataTable.Columns.Contains("档位"))
                {
                    _AxialFanParameters.Gears = FuncStr.NullToStr(_DataTable.Rows[i]["档位"]);
                }

                _List.Add(_AxialFanParameters);
            }
            return _List;
        }

        private List<FanParameters> InitFanParameters(DataTable _DataTable)
        {
            var _List = new List<FanParameters>();
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                FanParameters _FanParameters = new FanParameters()
                {
                    Suffix = FuncStr.NullToStr(_DataTable.Rows[i]["后三位"]),
                    No = FuncStr.NullToStr(_DataTable.Rows[i]["机号"]),
                    CCCF_Spec = FuncStr.NullToStr(_DataTable.Rows[i]["CCCF规格"]),
                    Rpm = FuncStr.NullToStr(_DataTable.Rows[i]["转速r/min"]),
                    AirVolume = FuncStr.NullToStr(_DataTable.Rows[i]["风量m^3/h"]),
                    Pa = FuncStr.NullToStr(_DataTable.Rows[i]["全压(Pa)"]),
                    StaticPressure = FuncStr.NullToStr(_DataTable.Rows[i]["静压(Pa)"]),
                    Power = FuncStr.NullToStr(_DataTable.Rows[i]["功率(Kw)"]),
                    Weight = FuncStr.NullToStr(_DataTable.Rows[i]["重量(Kg)"]),
                    Noise = FuncStr.NullToStr(_DataTable.Rows[i]["噪声dB(A)"]),
                    Height = FuncStr.NullToStr(_DataTable.Rows[i]["高"]),
                    Length = FuncStr.NullToStr(_DataTable.Rows[i]["长"]),
                    Width = FuncStr.NullToStr(_DataTable.Rows[i]["宽"]),
                    Height1 = FuncStr.NullToStr(_DataTable.Rows[i]["高_1"]),
                    Length1 = FuncStr.NullToStr(_DataTable.Rows[i]["长_1"]),
                    Height2 = FuncStr.NullToStr(_DataTable.Rows[i]["高_2"]),
                    Length2 = FuncStr.NullToStr(_DataTable.Rows[i]["长_2"]),
                    Width1 = FuncStr.NullToStr(_DataTable.Rows[i]["宽_1"]),
                    OutletWidth = FuncStr.NullToStr(_DataTable.Rows[i]["出风口宽度"]),
                    AirOutletHeight = FuncStr.NullToStr(_DataTable.Rows[i]["出风口高度"]),
                    AirInletWidth = FuncStr.NullToStr(_DataTable.Rows[i]["进风口宽度"]),
                    AirInletHeight = FuncStr.NullToStr(_DataTable.Rows[i]["进风口高度"]),
                    TheWindSpeed = FuncStr.NullToStr(_DataTable.Rows[i]["出风风速"]),
                    DynamicPressure = FuncStr.NullToStr(_DataTable.Rows[i]["动压"]),
                    RealPower = FuncStr.NullToStr(_DataTable.Rows[i]["实际功率"]),
                    FanEfficiency = FuncStr.NullToStr(_DataTable.Rows[i]["风机效率"]),
                    MotorPower = FuncStr.NullToStr(_DataTable.Rows[i]["电机功率"])

                };

                if (_DataTable.Columns.Contains("档位"))
                {
                    _FanParameters.Gears = FuncStr.NullToStr(_DataTable.Rows[i]["档位"]);
                }

                _List.Add(_FanParameters);
            }

            return _List;
        }

        private List<FanSelectionData> InitFanSelection(DataTable _DataTable)
        {
            var _List = new List<FanSelectionData>();
            var _StrColumns = GetColumnsByDataTable(_DataTable);
            for (int i = 0; i < _DataTable.Rows.Count; i++)
            {
                var _Y = FuncStr.NullToStr(_DataTable.Rows[i][0]);
                for (int j = 0; j < _StrColumns.Count(); j++)
                {
                    if (FuncStr.NullToStr(_StrColumns[j]).Contains("Column")) { continue; }
                    //if (FuncStr.NullToStr(_DataTable.Rows[i][_StrColumns[j]]) == string.Empty) { continue; }
                    FanSelectionData _FanSelection = new FanSelectionData();
                    _FanSelection.X = _StrColumns[j];
                    _FanSelection.Y = _Y;
                    _FanSelection.Value = FuncStr.NullToStr(_DataTable.Rows[i][_StrColumns[j]]);
                    _List.Add(_FanSelection);
                }
            }
            return _List;
        }

        public string[] GetColumnsByDataTable(DataTable _DataTable)
        {
            string[] _StrColumns = null;

            if (_DataTable.Columns.Count > 0)
            {
                int _ColumnNum = 0;
                _ColumnNum = _DataTable.Columns.Count;
                _StrColumns = new string[_ColumnNum];
                for (int i = 0; i < _DataTable.Columns.Count; i++)
                {
                    _StrColumns[i] = _DataTable.Columns[i].ColumnName;
                }
            }
            return _StrColumns;
        }


    }
}
