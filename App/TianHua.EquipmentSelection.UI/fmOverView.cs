using AcHelper;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using System;
using OfficeOpenXml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using ThCADExtension;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TianHua.FanSelection.ExcelExport;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI
{
    public partial class fmOverView : DevExpress.XtraEditors.XtraForm
    {
        public List<FanDataModel> m_ListFan { get; set; }

        public List<FanDataModel> m_ListFanRoot = new List<FanDataModel>();

        public List<FanDataModel> m_ListMainFan { get; set; }


        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);

        /// <summary>
        /// 风机箱参数
        /// </summary>
        public List<FanParameters> m_ListFanParameters = new List<FanParameters>();


        /// <summary>
        /// 风机箱参数-单速
        /// </summary>
        public List<FanParameters> m_ListFanParametersSingle = new List<FanParameters>();


        /// <summary>
        /// 风机箱参数 双速
        /// </summary>
        public List<FanParameters> m_ListFanParametersDouble = new List<FanParameters>();


        /// <summary>
        /// 轴流风机参数
        /// </summary>
        public List<AxialFanParameters> m_ListAxialFanParameters = new List<AxialFanParameters>();

        /// <summary>
        /// 轴流风机参数 双速
        /// </summary>
        public List<AxialFanParameters> m_ListAxialFanParametersDouble = new List<AxialFanParameters>();

        public fmOverView()
        {
            InitializeComponent();

            foreach (Control _Ctrl in this.layoutControl2.Controls)
            {
                if (_Ctrl is CheckEdit)
                {
                    var _Edit = _Ctrl as CheckEdit;
                    if (_Edit.Name == "CheckAll") continue;
                    _Edit.CheckedChanged += Check_CheckedChanged;

                    _Edit.EditValueChanged += _Edit_EditValueChanged; ;
                }
            }

        }




        /// <summary>
        /// 单例
        /// </summary>
        private static fmOverView SingleOverViewDialog;
        public static fmOverView GetInstance()
        {
            if (SingleOverViewDialog == null)
            {
                SingleOverViewDialog = new fmOverView();
            }
            return SingleOverViewDialog;
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }



        public void Init(List<FanDataModel> _ListFan, List<FanParameters> _ListFanParameters, List<FanParameters> _ListFanParametersSingle,
           List<FanParameters> _ListFanParametersDouble, List<AxialFanParameters> _ListAxialFanParameters, List<AxialFanParameters> _ListAxialFanParametersDouble)
        {
            m_ListMainFan = _ListFan;
            var _Json = FuncJson.Serialize(_ListFan);
            m_ListFan = FuncJson.Deserialize<List<FanDataModel>>(_Json);

            //if (m_ListFan != null && m_ListFan.Count > 0)
            //{
            //    m_ListFan.ForEach(p =>
            //    {
            //        var _List = m_ListFan.FindAll(s => p.InstallSpace == s.InstallSpace && p.InstallFloor == s.InstallFloor && p.ID != s.ID);
            //        if (_List != null && _List.Count > 0) p.IsRepetitions = true;
            //    });
            //}


            m_ListFanParameters = _ListFanParameters;
            m_ListFanParametersSingle = _ListFanParametersSingle;
            m_ListFanParametersDouble = _ListFanParametersDouble;
            m_ListAxialFanParameters = _ListAxialFanParameters;
            m_ListAxialFanParametersDouble = _ListAxialFanParametersDouble;

            InitListFan();
        }




        private void _Edit_EditValueChanged(object sender, EventArgs e)
        {
            string _FilterString = FilterOverView();

            TreeList.ActiveFilterString = _FilterString;
        }


        private void Check_CheckedChanged(object sender, EventArgs e)
        {
            CheckEdit _CheckEdit = sender as CheckEdit;
            if (_CheckEdit.Checked == true)
            {
                foreach (Control _Ctrl in this.layoutControl2.Controls)
                {
                    if (_Ctrl is CheckEdit)
                    {
                        var _Edit = _Ctrl as CheckEdit;
                        if (_Edit.Name != "CheckAll" && _Edit.Checked == false)
                            return;
                    }
                }
                this.CheckAll.CheckedChanged -= new System.EventHandler(this.CheckAll_CheckedChanged);
                CheckAll.Checked = true;
                this.CheckAll.CheckedChanged += new System.EventHandler(this.CheckAll_CheckedChanged);
            }
            else
            {
                this.CheckAll.CheckedChanged -= new System.EventHandler(this.CheckAll_CheckedChanged);
                CheckAll.EditValue = false;
                this.CheckAll.CheckedChanged += new System.EventHandler(this.CheckAll_CheckedChanged);
            }





        }

        private string FilterOverView()
        {
            List<string> _List = new List<string>();
            foreach (Control _Ctrl in this.layoutControl2.Controls)
            {
                if (_Ctrl is CheckEdit)
                {
                    var _Edit = _Ctrl as CheckEdit;
                    if (_Edit.Checked)
                        _List.Add(_Edit.Text);
                }
            }

            var _FilterString = string.Empty;

            if (_List != null && _List.Count > 0)
            {
                for (int i = 0; i < _List.Count; i++)
                {
                    if (i == 0)
                        _FilterString = @" ( Scenario =  '" + FuncStr.NullToStr(_List[i]) + "'";
                    else
                        _FilterString += @" OR Scenario =  '" + FuncStr.NullToStr(_List[i]) + "'";
                }

            }

            if (_FilterString == string.Empty) { _FilterString = " 1 <> 1 "; }

            _FilterString += @") AND IsErased = false ";

            return _FilterString;
        }

        private void fmOverView_Load(object sender, EventArgs e)
        {

        }

        private void InitListFan()
        {
            m_ListFan.ForEach(p =>
            {
                if (p.PID == "0")
                {
                    var _FanPrefix = PubVar.g_ListFanPrefixDict.Find(s => s.FanUse == p.Scenario);
                    if (_FanPrefix != null)
                        p.PID = FuncStr.NullToStr(_FanPrefix.No);
                }
            });

            m_ListFanRoot = new List<FanDataModel>();
            if (PubVar.g_ListFanPrefixDict != null && PubVar.g_ListFanPrefixDict.Count > 0)
            {
                for (int i = 0; i < PubVar.g_ListFanPrefixDict.Count; i++)
                {
                    FanDataModel _FanDataModel = new FanDataModel();
                    _FanDataModel.ID = PubVar.g_ListFanPrefixDict[i].No.ToString();
                    _FanDataModel.PID = "0";
                    _FanDataModel.SortID = PubVar.g_ListFanPrefixDict[i].No;
                    _FanDataModel.SortScenario = PubVar.g_ListFanPrefixDict[i].No;
                    _FanDataModel.Scenario = PubVar.g_ListFanPrefixDict[i].FanUse;
                    m_ListFanRoot.Add(_FanDataModel);
                }
            }

            m_ListFan.AddRange(m_ListFanRoot);



            if (m_ListFan != null && m_ListFan.Count > 0)
            {
                m_ListFan.ForEach(p =>
                {
                    var _List = m_ListFan.FindAll(s => p.InstallSpace == s.InstallSpace && p.InstallFloor == s.InstallFloor && p.ID != s.ID && p.FanPrefix == s.FanPrefix);
                    if (_List != null && _List.Count > 0)
                    {
                        _List.ForEach(l =>
                        {
                            if (l.ListVentQuan != null && l.ListVentQuan.Count > 0)
                            {
                                for (int i = 0; i < l.ListVentQuan.Count; i++)
                                {

                                    if (p.ListVentQuan.Contains(l.ListVentQuan[i]))
                                    {
                                        p.IsRepetitions = true;
                                    }

                                }
                            }
                        });
                    }




                });
            }



            this.TreeList.ParentFieldName = "PID";
            this.TreeList.KeyFieldName = "ID";
            if (m_ListFan != null && m_ListFan.Count > 0)
                m_ListFan = m_ListFan.OrderBy(p => p.SortID).ToList();
            TreeList.DataSource = m_ListFan;
            this.TreeList.ExpandAll();
        }

        private void TreeList_CustomColumnDisplayText(object sender, DevExpress.XtraTreeList.CustomColumnDisplayTextEventArgs e)
        {
            if (e == null || e.Node == null) { return; }

            if (e.Column.FieldName == "Scenario" || e.Column.FieldName == "OverViewFanNum")
            {
                var _ID = FuncStr.NullToStr(e.Node.GetValue("ID"));
                var _Fan = m_ListFan.Find(p => p.ID == _ID);
                if (_Fan == null) { return; }

                if (IsGUID(_Fan.PID))
                {
                    e.DisplayText = " - ";
                }

            }
            if (e.Column.FieldName == "VentQuan" || e.Column.FieldName == "AirVolume" || e.Column.FieldName == "WindResis" || e.Column.FieldName == "SysAirVolume")
            {
                var _PID = FuncStr.NullToStr(e.Node.GetValue("PID"));
                //var _Fan = m_ListFan.Find(p => p.ID == _ID);
                //if (_Fan == null) { e.DisplayText = string.Empty;  return; }
                if (_PID == "0")
                    e.DisplayText = string.Empty;
            }
        }


        public bool IsGUID(string _Expression)
        {
            if (_Expression != null)
            {
                Regex _GuidRegEx = new Regex(@"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$");
                return _GuidRegEx.IsMatch(_Expression);
            }
            return false;
        }

        private void CheckAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (Control _Ctrl in this.layoutControl2.Controls)
            {
                if (_Ctrl is CheckEdit)
                {

                    var _Edit = _Ctrl as CheckEdit;
                    _Edit.Checked = CheckAll.Checked;

                }
            }

            string _FilterString = FilterOverView();

            TreeList.ActiveFilterString = _FilterString;
        }

        private void Check_EditValueChanging(object sender, DevExpress.XtraEditors.Controls.ChangingEventArgs e)
        {

        }

        private void TreeList_CustomDrawNodeCell(object sender, DevExpress.XtraTreeList.CustomDrawNodeCellEventArgs e)
        {
            FanDataModel _Fan = TreeList.GetDataRecordByNode(e.Node) as FanDataModel;
            if (_Fan == null|| e.Column == null) return;
            if (e.Column.FieldName == "Scenario")
            {
                if (_Fan.PID == "0")
                {
                    e.Appearance.ForeColor = Color.FromArgb(27, 161, 226);
                    e.Appearance.Font = new System.Drawing.Font(e.Appearance.Font, e.Appearance.Font.Style | FontStyle.Bold);
                }
            }

            if (e.Column.FieldName == "OverViewFanNum")
            {
                if (_Fan.IsRepetitions)
                {
                    e.Appearance.ForeColor = Color.FromArgb(208, 70, 38);
                }
            }

        }

        private void BarBtnExportFanPara_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            List<string> _ListSceneScreening = GetSceneScreening();
            using (var excelpackage = ThFanSelectionUIUtils.CreateModelExportExcelPackage())
            {
                //Microsoft.Office.Interop.Excel.Application _ExclApp = new Microsoft.Office.Interop.Excel.Application();
                //_ExclApp.DisplayAlerts = false;
                //_ExclApp.Visible = false;
                //_ExclApp.ScreenUpdating = false;
                //Microsoft.Office.Interop.Excel.Workbook _WorkBook = _ExclApp.Workbooks.Open(_ImportExcelPath, System.Type.Missing, System.Type.Missing, System.Type.Missing,
                //  System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing,
                //System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);

                var _Sheet = excelpackage.Workbook.Worksheets[0];

                var _List = GetListExportFanPara();

                if (_List == null || _List.Count == 0) { return; }

                if (_ListSceneScreening != null && _ListSceneScreening.Count > 0)
                {
                    _List = _List.FindAll(p => _ListSceneScreening.Contains(p.Scenario));
                }

                if (_List != null && _List.Count > 0) _List = _List.OrderBy(p => p.SortScenario).OrderBy(p => p.SortID).ToList();

                var i = 4;

                _List.ForEach(p =>
                {
                    _Sheet.Cells[i, 1].Value = p.No;
                    _Sheet.Cells[i, 2].Value = p.Coverage;
                    _Sheet.Cells[i, 3].Value = p.FanForm;
                    _Sheet.Cells[i, 4].Value = p.CalcAirVolume;
                    _Sheet.Cells[i, 5].Value = p.FanDelivery;
                    _Sheet.Cells[i, 6].Value = p.Pa;
                    _Sheet.Cells[i, 7].Value = FuncStr.NullToInt(p.StaticPa);
                    _Sheet.Cells[i, 8].Value = p.FanEnergyLevel;
                    _Sheet.Cells[i, 9].Value = p.FanEfficiency;
                    _Sheet.Cells[i, 10].Value = p.FanRpm;
                    _Sheet.Cells[i, 11].Value = p.DriveMode;

                    _Sheet.Cells[i, 12].Value = p.ElectricalEnergyLevel;
                    _Sheet.Cells[i, 13].Value = p.MotorPower;
                    _Sheet.Cells[i, 14].Value = p.PowerSource;
                    _Sheet.Cells[i, 15].Value = p.ElectricalRpm;
                    _Sheet.Cells[i, 16].Value = p.IsDoubleSpeed;
                    _Sheet.Cells[i, 17].Value = p.IsFrequency;
                    _Sheet.Cells[i, 18].Value = p.WS;
                    _Sheet.Cells[i, 19].Value = p.IsFirefighting;


                    _Sheet.Cells[i, 20].Value = p.dB;
                    _Sheet.Cells[i, 21].Value = p.Weight;
                    _Sheet.Cells[i, 22].Value = p.Length;
                    _Sheet.Cells[i, 23].Value = p.Width;
                    _Sheet.Cells[i, 24].Value = p.Height;



                    _Sheet.Cells[i, 25].Value = p.VibrationMode;
                    _Sheet.Cells[i, 26].Value = p.Amount;
                    _Sheet.Cells[i, 27].Value = p.Remark;

                    i++;
                });

                SaveFileDialog _SaveFileDialog = new SaveFileDialog();
                _SaveFileDialog.Filter = "Xlsx Files(*.xlsx)|*.xlsx";
                _SaveFileDialog.RestoreDirectory = true;
                _SaveFileDialog.FileName = "风机参数表 - " + DateTime.Now.ToString("yyyy.MM.dd HH.mm");
                _SaveFileDialog.InitialDirectory = Active.DocumentDirectory;
                var _DialogResult = _SaveFileDialog.ShowDialog();

                if (_DialogResult == DialogResult.OK)
                {
                    TreeList.PostEditor();
                    var _FilePath = _SaveFileDialog.FileName.ToString();
                    excelpackage.SaveAs(new FileInfo(_FilePath));
                }
            }
        }

        private List<ExportFanParaModel> GetListExportFanPara()
        {
            List<ExportFanParaModel> _List = new List<ExportFanParaModel>();
            m_ListMainFan.ForEach(p =>
           {
               if (p.FanModelName == string.Empty || p.FanModelName == "无此风机") { return; }
               var _FanPrefixDict = PubVar.g_ListFanPrefixDict.Find(s => s.FanUse == p.Scenario);
               if (_FanPrefixDict == null) return;
               if (p.IsErased) return;
               ExportFanParaModel _ExportFanPara = new ExportFanParaModel();
               _ExportFanPara.ID = p.ID;
               _ExportFanPara.Scenario = p.Scenario;
               _ExportFanPara.SortScenario = _FanPrefixDict.No;
               _ExportFanPara.SortID = p.SortID;
               _ExportFanPara.No = p.FanNum;
               _ExportFanPara.Coverage = p.Name;
               if (p.VentStyle != null)
                   _ExportFanPara.FanForm = p.VentStyle.Replace("(电机内置)", "").Replace("(电机外置)", "");
               //_ExportFanPara.CalcAirVolume = FuncStr.NullToStr(p.AirVolume);

               if (p.IsManualInputAirVolume)
               {
                   _ExportFanPara.CalcAirVolume = "-";
               }
               else
               {
                   _ExportFanPara.CalcAirVolume = FuncStr.NullToStr(p.AirCalcValue);
               }
               _ExportFanPara.CalcAirVolume = FuncStr.NullToStr(p.AirCalcValue);
               _ExportFanPara.FanEnergyLevel = p.VentLev;
               _ExportFanPara.DriveMode = p.VentConnect;
               _ExportFanPara.ElectricalEnergyLevel = p.EleLev;
               _ExportFanPara.MotorPower = p.FanModelMotorPower;
               _ExportFanPara.PowerSource = "380-3-50";
               _ExportFanPara.ElectricalRpm = FuncStr.NullToStr(p.MotorTempo);
               _ExportFanPara.IsDoubleSpeed = p.Control;
               _ExportFanPara.IsFrequency = p.IsFre ? "是" : "否";
               _ExportFanPara.WS = p.FanModelPower;
               _ExportFanPara.IsFirefighting = p.PowerType == "消防" ? "Y" : "N";
               _ExportFanPara.VibrationMode = p.VibrationMode;
               _ExportFanPara.Amount = FuncStr.NullToStr(p.VentQuan);
               _ExportFanPara.Remark = p.Remark;
               _ExportFanPara.FanEfficiency = p.FanInternalEfficiency;
               _ExportFanPara.StaticPa = FuncStr.NullToStr((p.DuctResistance + p.Damper) * p.SelectionFactor);
               if (FuncStr.NullToStr(p.VentStyle) == "轴流")
               {
                   List<AxialFanParameters> _ListAxialFanParameters = GetAxialFanParametersByControl(p);
                   var _FanParameters = _ListAxialFanParameters.Find(s => s.No == FuncStr.NullToStr(p.FanModelID) && s.ModelNum == p.FanModelName);
                   if (_FanParameters == null) return;
                   _ExportFanPara.FanDelivery = FuncStr.NullToStr(p.AirVolume);
                   _ExportFanPara.Pa = FuncStr.NullToStr(p.WindResis);

                   _ExportFanPara.FanRpm = _FanParameters.Rpm;
                   _ExportFanPara.dB = _FanParameters.Noise;
                   _ExportFanPara.Weight = _FanParameters.Weight;
                   _ExportFanPara.Length = _FanParameters.Length;
                   _ExportFanPara.Width = _FanParameters.Diameter;
                   _ExportFanPara.Height = string.Empty;
               }
               else
               {
                   List<FanParameters> _ListFanParameters = GetFanParametersByControl(p);
                   var _FanParameters = _ListFanParameters.Find(s => s.Suffix == FuncStr.NullToStr(p.FanModelID) && s.CCCF_Spec == p.FanModelName);
                   if (_FanParameters == null) return;
                   _ExportFanPara.FanDelivery = FuncStr.NullToStr(p.AirVolume);
                   _ExportFanPara.Pa = FuncStr.NullToStr(p.WindResis);
                   _ExportFanPara.FanRpm = _FanParameters.Rpm;
                   _ExportFanPara.dB = _FanParameters.Noise;
                   _ExportFanPara.Weight = _FanParameters.Weight;
                   _ExportFanPara.Length = _FanParameters.Length;
                   _ExportFanPara.Width = _FanParameters.Weight;
                   _ExportFanPara.Height = _FanParameters.Height;
               }

               if (p.Control == "双速")
               {
                   var _SonFan = m_ListFan.Find(s => s.PID == p.ID);

                   if (_SonFan != null)
                   {
                       if (p.IsManualInputAirVolume && _SonFan.IsManualInputAirVolume)
                       {
                           _ExportFanPara.CalcAirVolume = "-/-";
                       }
                       else if (p.IsManualInputAirVolume)
                       {
                           _ExportFanPara.CalcAirVolume = "-/" + FuncStr.NullToStr(_SonFan.AirCalcValue);

                       }
                       else if (_SonFan.IsManualInputAirVolume)
                       {
                           _ExportFanPara.CalcAirVolume = FuncStr.NullToStr(p.AirCalcValue) + "/-";
                       }
                       else
                       {
                           _ExportFanPara.CalcAirVolume = FuncStr.NullToStr(p.AirCalcValue) + "/" + FuncStr.NullToStr(_SonFan.AirCalcValue);
                       }


                       _ExportFanPara.FanDelivery = FuncStr.NullToStr(p.AirVolume) + "/" + FuncStr.NullToStr(_SonFan.AirVolume);

                       _ExportFanPara.Pa = FuncStr.NullToStr(p.WindResis) + "/" + FuncStr.NullToStr(_SonFan.WindResis);

                       _ExportFanPara.StaticPa = FuncStr.NullToInt(FuncStr.NullToStr((p.DuctResistance + p.Damper) * p.SelectionFactor)) + "/" + FuncStr.NullToInt(FuncStr.NullToStr((_SonFan.DuctResistance + _SonFan.Damper) * _SonFan.SelectionFactor));

                   }
               }


               _List.Add(_ExportFanPara);
           });
            return _List;
        }

        private List<AxialFanParameters> GetAxialFanParametersByControl(FanDataModel p)
        {
            List<AxialFanParameters> _ListAxialFanParameters = new List<AxialFanParameters>();
            if (p.Control == "双速")
            {
                _ListAxialFanParameters = m_ListAxialFanParametersDouble;
            }
            else
            {
                _ListAxialFanParameters = m_ListAxialFanParameters;
            }

            return _ListAxialFanParameters;
        }

        private List<FanParameters> GetFanParametersByControl(FanDataModel p)
        {
            List<FanParameters> _ListFanParameters = new List<FanParameters>();
            if (p.Control == "双速")
            {
                _ListFanParameters = m_ListFanParametersDouble;
            }
            else
            {
                _ListFanParameters = m_ListFanParameters;
            }
            if (FuncStr.NullToStr(p.VentStyle).Contains("后倾离心"))
            {
                _ListFanParameters = m_ListFanParametersSingle;
            }
            return _ListFanParameters;
        }

        private List<string> GetSceneScreening()
        {
            List<string> _List = new List<string>();
            foreach (Control _Ctrl in this.layoutControl2.Controls)
            {
                if (_Ctrl is CheckEdit)
                {
                    var _Edit = _Ctrl as CheckEdit;
                    if (_Edit.Checked)
                        _List.Add(_Edit.Text);
                }
            }

            return _List;
        }

        private void BarBtnExportFanCalc_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            List<string> _ListSceneScreening = GetSceneScreening();
            using (var targetExcelPackage = ThFanSelectionUIUtils.CreateModelCalculateExcelPackage())
            using (var SmokeProofSourcePackage = ThFanSelectionUIUtils.CreateSmokeProofExcelPackage())
            using (var ExhaustSourcePackage = ThFanSelectionUIUtils.CreateSmokeDischargeExcelPackage())
            {
                var _Sheet = targetExcelPackage.Workbook.Worksheets[0];
                var targetsheet = targetExcelPackage.Workbook.Worksheets["防烟计算"];

                var _List = m_ListMainFan;

                if (_ListSceneScreening != null && _ListSceneScreening.Count > 0)
                {
                    _List = _List.FindAll(p => _ListSceneScreening.Contains(p.Scenario) && !p.IsErased);
                }

                if (_List != null && _List.Count > 0) _List = _List.OrderBy(p => p.SortScenario).OrderBy(p => p.SortID).ToList();

                var i = 4;
                ExcelRangeCopyOperator copyOperatorForVolumeModel = new ExcelRangeCopyOperator();
                ExcelRangeCopyOperator copyOperatorForExhaustModel = new ExcelRangeCopyOperator();
                _List.ForEach(p =>
                {
                    if (p.FanModelName == string.Empty || p.FanModelName == "无此风机") { return; }
                    var _FanPrefixDict = PubVar.g_ListFanPrefixDict.Find(s => s.FanUse == p.Scenario);
                    if (_FanPrefixDict == null) return;
                    if (p.PID != "0") { return; }
                    _Sheet.Cells[i, 1].Value = p.FanNum;
                    _Sheet.Cells[i, 2].Value = p.Name;
                    _Sheet.Cells[i, 3].Value = p.Scenario;

                    if (p.IsManualInputAirVolume)
                    {
                        _Sheet.Cells[i, 13].Value = "-";
                    }
                    else
                    {
                        _Sheet.Cells[i, 13].Value = p.AirCalcValue;
                    }
                    _Sheet.Cells[i, 14].Value = p.AirVolume;
                    _Sheet.Cells[i, 15].Value = p.DuctLength;

                    _Sheet.Cells[i, 16].Value = p.Friction;
                    _Sheet.Cells[i, 17].Value = p.LocRes;

                    _Sheet.Cells[i, 18].Value = p.DuctResistance;

                    _Sheet.Cells[i, 19].Value = p.Damper;
                    _Sheet.Cells[i, 20].Value = p.EndReservedAirPressure;
                    _Sheet.Cells[i, 21].Value = p.DynPress;


                    _Sheet.Cells[i, 22].Value = p.CalcResistance;
                    _Sheet.Cells[i, 23].Value = p.WindResis;

                    _Sheet.Cells[i, 24].Value = p.FanModelPower;

                    if (p.Control == "双速")
                    {
                        var _SonFan = m_ListFan.Find(s => s.PID == p.ID);

                        if (_SonFan != null)
                        {
                            i++;
                            _Sheet.Cells[i, 1].Value = _SonFan.FanNum;
                            _Sheet.Cells[i, 2].Value = "-";
                            _Sheet.Cells[i, 3].Value = "-";



                            if (_SonFan != null)
                            {
                                if (_SonFan.IsManualInputAirVolume)
                                {
                                    _Sheet.Cells[i, 13].Value = "-";
                                }
                                else
                                {
                                    _Sheet.Cells[i, 13].Value = _SonFan.AirCalcValue;
                                }



                                _Sheet.Cells[i, 13].Value = _SonFan.AirCalcValue;
                                _Sheet.Cells[i, 14].Value = _SonFan.AirVolume;
                                _Sheet.Cells[i, 15].Value = _SonFan.DuctLength;

                                _Sheet.Cells[i, 16].Value = _SonFan.Friction;
                                _Sheet.Cells[i, 17].Value = _SonFan.LocRes;

                                _Sheet.Cells[i, 18].Value = _SonFan.DuctResistance;

                                _Sheet.Cells[i, 19].Value = _SonFan.Damper;
                                _Sheet.Cells[i, 20].Value = _SonFan.EndReservedAirPressure;
                                _Sheet.Cells[i, 21].Value = _SonFan.DynPress;

                                _Sheet.Cells[i, 22].Value = _SonFan.CalcResistance;
                                _Sheet.Cells[i, 23].Value = _SonFan.WindResis;
                                _Sheet.Cells[i, 24].Value = _SonFan.FanModelPower;
                            }
                        }
                    }

                    if (!p.IsNull())
                    {
                        ExcelExportEngine.Instance.Model = p;
                        if (p.FanVolumeModel != null && p.IsManualInputAirVolume != true)
                        {
                            ExcelExportEngine.Instance.RangeCopyOperator = copyOperatorForVolumeModel;
                            ExcelExportEngine.Instance.Sourcebook = SmokeProofSourcePackage.Workbook;
                            ExcelExportEngine.Instance.Targetsheet = targetExcelPackage.Workbook.Worksheets["防烟计算"];
                            ExcelExportEngine.Instance.Run();
                        }
                        else if (p.ExhaustModel != null && p.IsManualInputAirVolume != true)
                        {
                            ExcelExportEngine.Instance.RangeCopyOperator = copyOperatorForExhaustModel;
                            ExcelExportEngine.Instance.Sourcebook = ExhaustSourcePackage.Workbook;
                            ExcelExportEngine.Instance.Targetsheet = targetExcelPackage.Workbook.Worksheets["排烟计算"];
                            ExcelExportEngine.Instance.RunExhaustExport();
                        }
                    }

                    i++;
                });

                SaveFileDialog _SaveFileDialog = new SaveFileDialog();
                _SaveFileDialog.Filter = "Xlsx Files(*.xlsx)|*.xlsx";
                _SaveFileDialog.RestoreDirectory = true;
                _SaveFileDialog.InitialDirectory = Active.DocumentDirectory;
                _SaveFileDialog.FileName = "风机计算书 - " + DateTime.Now.ToString("yyyy.MM.dd HH.mm");
                var DialogResult = _SaveFileDialog.ShowDialog();

                if (DialogResult == DialogResult.OK)
                {
                    TreeList.PostEditor();
                    var _FilePath = _SaveFileDialog.FileName.ToString();

                    targetExcelPackage.SaveAs(new FileInfo(_FilePath));
                }
            }
        }

        public void DataSourceChanged(List<FanDataModel> _List)
        {
            if (_List == null || _List.Count == 0) { return; }
            m_ListMainFan = _List;
            var _Json = FuncJson.Serialize(_List);
            m_ListFan = FuncJson.Deserialize<List<FanDataModel>>(_Json);


            InitListFan();
            _Edit_EditValueChanged(null, null);
        }

        private void TreeList_MouseDown(object sender, MouseEventArgs e)
        {
            TreeListHitInfo _HitInfo = (sender as TreeList).CalcHitInfo(new System.Drawing.Point(e.X, e.Y));

            TreeListNode _Node = _HitInfo.Node;
            if (e.Button == MouseButtons.Left)
            {
                if (_Node != null && _HitInfo.Column != null)
                {
                    TreeList.FocusedColumn = _HitInfo.Column;

                    if (_HitInfo.Column.FieldName == "OverViewFanNum")
                    {
                        _Node.TreeList.FocusedNode = _Node;

                        var _Fan = _Node.TreeList.GetFocusedRow() as FanDataModel;

                        if (_Fan == null) { return; }

                        string _ErrorStr = string.Empty;

                        if (_Fan.IsRepetitions)
                        {
                            var _List = m_ListMainFan.FindAll(p => p.InstallSpace == _Fan.InstallSpace && p.InstallFloor == _Fan.InstallFloor && p.ID != _Fan.ID && p.FanPrefix == _Fan.FanPrefix);

                            if (_List != null && _List.Count > 0)
                            {
                                _List.ForEach(p =>
                                {
                                    if (p.ListVentQuan != null && p.ListVentQuan.Count > 0)
                                    {
                                        for (int i = 0; i < p.ListVentQuan.Count; i++)
                                        {

                                            if (_Fan.ListVentQuan.Contains(p.ListVentQuan[i]))
                                            {
                                                if (_ErrorStr == string.Empty)
                                                    _ErrorStr = p.FanNum;
                                                else
                                                    _ErrorStr += " , " + p.FanNum;
                                                break;
                                            }
                                        }
                                    }
                                });

                            }

                        }

                        if (_ErrorStr != string.Empty)
                        {
                            this.ToolTip.ShowHint("当前风机与[" + _ErrorStr + "]冲突！", MousePosition);
                            return;
                        }

                    }


                }
            }
        }

        private void TreeList_MouseClick(object sender, MouseEventArgs e)
        {
            TreeListHitInfo _HitInfo = (sender as TreeList).CalcHitInfo(new System.Drawing.Point(e.X, e.Y));

            TreeListNode _Node = _HitInfo.Node;

            if (e.Button == MouseButtons.Left)
            {
            }
        }
    }
}
