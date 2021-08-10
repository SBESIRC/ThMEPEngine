using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Function;
using TianHua.FanSelection.ExcelExport;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using TianHua.FanSelection.UI.IO;
using TianHua.FanSelection.UI.CAD;
using TianHua.FanSelection.Messaging;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI
{
    public partial class fmFanSelection : XtraForm, IFanSelection
    {

        public PresentersFanSelection m_Presenter;

        public List<FanDataModel> m_ListFan { get; set; }

        public List<string> m_ListScenario { get; set; }

        public List<string> m_ListVentStyle { get; set; }

        public List<string> m_ListVentConnect { get; set; }

        public List<string> m_ListVentLev { get; set; }

        public List<string> m_ListEleLev { get; set; }

        public List<int> m_ListMotorTempo { get; set; }

        public List<string> m_ListMountType { get; set; }

        public DataManager m_DataMgr = new DataManager();

        public fmFanModel m_fmFanModel = new fmFanModel();

        public fmOverView m_fmOverView = fmOverView.GetInstance();

        public List<string> m_ListSceneScreening { get; set; }
        public Action<ThModelCopyMessage> OnModelCopiedHandler
        {
            get
            {
                return OnModelCopied;
            }
        }
        public Action<ThModelDeleteMessage> OnModelDeletedHandler
        {
            get
            {
                return OnModelDeleted;
            }
        }
        public Action<ThModelBeginSaveMessage> OnModelBeginSaveHandler
        {
            get
            {
                return OnModelBeginSave;
            }
        }
        public Action<ThModelUndoMessage> OnModelUndoHandler
        {
            get
            {
                return OnModelUndo;
            }
        }

        /// <summary>
        /// 风机箱选型
        /// </summary>
        public List<FanSelectionData> m_ListFanSelection = new List<FanSelectionData>();
        /// <summary>
        /// 轴流风机选型
        /// </summary>
        public List<FanSelectionData> m_ListAxialFanSelection = new List<FanSelectionData>();
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

        public void ResetPresenter()
        {
            if (m_Presenter != null)
            {
                this.Dispose();
                m_Presenter = null;
            }
            m_Presenter = new PresentersFanSelection(this);
        }

        public void ShowFormByID(string _ID)
        {
            var _FocusFan = m_ListFan.Find(p => p.ID == _ID);
            if (_FocusFan != null)
            {
                ComBoxScene.EditValue = _FocusFan.Scenario;
                TreeListNode _Node = TreeList.FindNodeByKeyID(_ID);
                TreeList.FocusedNode = _Node;
            }

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            InitForm();
        }

        public fmFanSelection()
        {
            InitializeComponent();
            UserLookAndFeel.Default.SetSkinStyle(SkinStyle.Office2013);
        }

        public void InitForm()
        {
            ResetPresenter();
            ComBoxScene.Properties.Items.Clear();
            ComBoxScene.Properties.Items.AddRange(m_ListScenario);
            ComBoxScene.EditValue = "厨房排油烟";

            ComBoxVentStyle.Items.Clear();
            ComBoxVentStyle.Items.AddRange(m_ListVentStyle);

            ComBoxVentConnect.Items.Clear();
            ComBoxVentConnect.Items.AddRange(m_ListVentConnect);

            ComBoxVentLev.Items.Clear();
            ComBoxVentLev.Items.AddRange(m_ListVentLev);

            ComBoxEleLev.Items.Clear();
            ComBoxEleLev.Items.AddRange(m_ListEleLev);

            ComBoxMotorTempo.Items.Clear();
            ComBoxMotorTempo.Items.AddRange(m_ListMotorTempo);

            ComBoxMountType.Items.Clear();
            ComBoxMountType.Items.AddRange(m_ListMountType);

            this.TreeList.ParentFieldName = "PID";
            this.TreeList.KeyFieldName = "ID";
            if (m_ListFan != null && m_ListFan.Count > 0)
                m_ListFan = m_ListFan.OrderBy(p => p.SortID).ToList();
            TreeList.DataSource = m_ListFan;
            this.TreeList.ExpandAll();

            //TreeList.Columns["SortID"].SortIndex = 0;
            //TreeList.Columns["SortID"].SortMode = DevExpress.XtraGrid.ColumnSortMode.Value;
            //TreeList.Columns["SortID"].SortOrder = SortOrder.Descending;

            InitData();
        }

        public void InitData()
        {
            var _JsonFanSelection = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Selection));
            m_ListFanSelection = FuncJson.Deserialize<List<FanSelectionData>>(_JsonFanSelection);

            var _JsonAxialFanSelection = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Selection));
            m_ListAxialFanSelection = FuncJson.Deserialize<List<FanSelectionData>>(_JsonAxialFanSelection);

            //离心-前倾-单速
            var _JsonFanParameters = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters));
            m_ListFanParameters = FuncJson.Deserialize<List<FanParameters>>(_JsonFanParameters);

            //离心-前倾-双速
            var _JsonFanParametersDouble = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters_Double));
            m_ListFanParametersDouble = FuncJson.Deserialize<List<FanParameters>>(_JsonFanParametersDouble);

            //离心-后倾-单速
            var _JsonFanParametersSingle = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters_Single));
            m_ListFanParametersSingle = FuncJson.Deserialize<List<FanParameters>>(_JsonFanParametersSingle);

            //轴流-单速
            var _JsonAxialFanParameters = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Parameters));
            m_ListAxialFanParameters = FuncJson.Deserialize<List<AxialFanParameters>>(_JsonAxialFanParameters);

            //轴流-双速
            var _JsonAxialFanParametersDouble = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Parameters_Double));
            m_ListAxialFanParametersDouble = FuncJson.Deserialize<List<AxialFanParameters>>(_JsonAxialFanParametersDouble);

            // 从图纸NOD中读取风机模型
            var dataSource = new ThFanModelDbDataSource();
            dataSource.Load(Active.Database);
            if (dataSource.Models.Count > 0)
            {
                m_ListFan = dataSource.Models.OrderBy(p => p.SortID).ToList();
            }else {
                // 若当前图纸NOD中没有风机模型，继续从json文件中读取
                var _JsonFile = Path.ChangeExtension(Active.DocumentFullPath, ".json");
                if (File.Exists(_JsonFile))
                {
                    var _JsonListFan = ReadTxt(_JsonFile);
                    m_ListFan = FuncJson.Deserialize<List<FanDataModel>>(_JsonListFan);
                }
            }
            this.TreeList.DataSource = m_ListFan;
            this.TreeList.ExpandAll();

            // 用当前图纸名更新标题
            string _Filename = Path.GetFileName(FuncStr.NullToStr(Active.DocumentName));
            this.Text = "风机选型 - " + Path.GetFileNameWithoutExtension(_Filename);
        }

        private void TreeList_CustomColumnDisplayText(object sender, DevExpress.XtraTreeList.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == "AirVolume")
            {
                var _ID = FuncStr.NullToStr(e.Node.GetValue("ID"));
                var _Fan = m_ListFan.Find(p => p.ID == _ID);
                if (_Fan == null) { return; }

                if (_Fan.AirVolume > 0)
                {
                    e.DisplayText = _Fan.AirVolume.ToString("#,##0");
                }


            }
            if (e.Column.FieldName == "VentQuan" || e.Column.FieldName == "MotorTempo")
            {
                var _ID = FuncStr.NullToStr(e.Node.GetValue("ID"));
                var _Fan = m_ListFan.Find(p => p.ID == _ID);
                if (_Fan == null) { return; }
                if (_Fan.PID != "0")
                {
                    e.DisplayText = "-";
                }
            }

            if (e.Column.FieldName == "FanModelName")
            {
                var _ID = FuncStr.NullToStr(e.Node.GetValue("ID"));
                var _Fan = m_ListFan.Find(p => p.ID == _ID);
                if (_Fan == null) { return; }
                if (_Fan.FanModelName == string.Empty && _Fan.AirVolume != 0 && _Fan.WindResis != 0)
                {
                    if (_Fan.PID == "0")
                    {
                        e.DisplayText = "无此风机";
                    }
                }
            }
        }

        private void PicRemark_Click(object sender, EventArgs e)
        {
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }
            fmRemark _fmRemark = new fmRemark();
            _fmRemark.m_Remark = _Fan.Remark;
            if (_fmRemark.ShowDialog() == DialogResult.OK)
            {
                _Fan.Remark = _fmRemark.m_Remark;

                if (_fmRemark.m_ApplyAll)
                {
                    m_ListFan.ForEach(p => p.Remark = _fmRemark.m_Remark);
                }


                TreeList.Refresh();
            }

        }

        private void TxtWindResis_Click(object sender, EventArgs e)
        {
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }
            fmDragCalc _fmDragCalc = new fmDragCalc();
            _fmDragCalc.InitForm(_Fan);
            if (_fmDragCalc.ShowDialog() == DialogResult.OK)
            {
                if (_fmDragCalc.m_ListFan != null && _fmDragCalc.m_ListFan.Count > 0)
                    _Fan = _fmDragCalc.m_ListFan.First();
                SetFanModel();
                //FanSelectionInfoError(_Fan);
                m_fmOverView.DataSourceChanged(m_ListFan);
            }
        }

        private void TxtAirVolume_Click(object sender, EventArgs e)
        {
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }
            if (_Fan.AirCalcFactor == 0)
            {
                if (_Fan.IsFireModel())
                {
                    _Fan.AirCalcFactor = 1.2;
                }
                else
                {
                    _Fan.AirCalcFactor = 1.1;
                }
            }

            //-消防排烟
            //-消防排烟兼平时排风的第一行
            if ((_Fan.Scenario == "消防排烟" || _Fan.Scenario == "消防排烟兼平时排风") && _Fan.PID == "0")
            {


                fmAirVolumeCalc_Exhaust _fmAirVolumeCalc = new fmAirVolumeCalc_Exhaust();

                _fmAirVolumeCalc.InitForm(_Fan);

                if (_fmAirVolumeCalc.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                _Fan.IsManualInputAirVolume = _fmAirVolumeCalc.CheckIsManualInput.Checked;

                if (_fmAirVolumeCalc.CheckIsManualInput.Checked)
                {
                    _Fan.SysAirVolume = FuncStr.NullToInt(_fmAirVolumeCalc.TxtManualInput.Text);

                    _Fan.AirVolume = _Fan.SysAirVolume;


                    _Fan.ExhaustModel = _fmAirVolumeCalc.Model.ExhaustModel;

                    _Fan.AirCalcFactor = _fmAirVolumeCalc.Model.AirCalcFactor;
                    _Fan.AirCalcValue = _fmAirVolumeCalc.Model.AirCalcValue;

                }
                else
                {
                    _Fan.ExhaustModel = _fmAirVolumeCalc.Model.ExhaustModel;
                    _Fan.SysAirVolume = _fmAirVolumeCalc.Model.SysAirVolume;
                    _Fan.AirCalcFactor = _fmAirVolumeCalc.Model.AirCalcFactor;
                    _Fan.AirCalcValue = _fmAirVolumeCalc.Model.AirCalcValue;

                }
                CheckSysCheckedChanged();
                SetFanModel();
                TreeList.Refresh();

            }
            else
            {
                fmAirVolumeCalc _fmAirVolumeCalc = new fmAirVolumeCalc();

                _fmAirVolumeCalc.InitForm(_Fan);
                if (_fmAirVolumeCalc.ShowDialog() == DialogResult.OK)
                {
                    if (_fmAirVolumeCalc.m_ListFan != null && _fmAirVolumeCalc.m_ListFan.Count > 0)
                    {
                        _Fan.IsManualInputAirVolume = _fmAirVolumeCalc.CheckIsManualInput.Checked;
                        if (_fmAirVolumeCalc.CheckIsManualInput.Checked)
                        {
                            _Fan.SysAirVolume = FuncStr.NullToInt(_fmAirVolumeCalc.TxtManualInput.Text);
                            _Fan.AirVolume = _Fan.SysAirVolume;

                        }
                        else
                        {
                            _Fan.SysAirVolume = _fmAirVolumeCalc.m_ListFan.First().SysAirVolume;
                            _Fan.AirCalcFactor = _fmAirVolumeCalc.m_ListFan.First().AirCalcFactor;
                            _Fan.AirCalcValue = _fmAirVolumeCalc.m_ListFan.First().AirCalcValue;
                            _Fan.FanVolumeModel = _fmAirVolumeCalc.m_ListFan.First().FanVolumeModel;
                            //_Fan.SysAirVolume = _Fan.AirVolume;

                        }


                    }
                    CheckSysCheckedChanged();
                    SetFanModel();
                    TreeList.Refresh();
                }
            }
        }

        private void TxtModelName_Click(object sender, EventArgs e)
        {
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }


            m_fmFanModel.InitForm(_Fan, m_ListFan);
            if (m_fmFanModel.ShowDialog() == DialogResult.OK)
            {
                if (m_fmFanModel.m_Fan != null)
                {
                    _Fan = m_fmFanModel.m_Fan;
                    SetFanModel();
                }

                TreeList.Refresh();
            }

        }

        private void TreeList_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }

            if (e.Column.FieldName == "VentNum")
            {
                var _Calculator = new VentSNCalculator(_Fan.VentNum);
                if (_Calculator.SerialNumbers.Count > 0)
                {
                    _Fan.ListVentQuan = _Calculator.SerialNumbers;
                    _Fan.VentQuan = _Fan.ListVentQuan.Count();
                }
                else
                {
                    _Fan.ListVentQuan = new List<int>() { 1 };
                    _Fan.VentNum = "1";
                    _Fan.VentQuan = 1;
                }

                CheckSysCheckedChanged();
            }

            if (e.Column.FieldName == "AirVolume")
            {
                var _Rem = FuncStr.NullToInt(e.Value) % 50;
                if (_Rem != 0)
                {
                    var _UnitsDigit = FindNum(FuncStr.NullToInt(e.Value), 1);

                    var _TensDigit = FindNum(FuncStr.NullToInt(e.Value), 2);

                    var _Tmp = FuncStr.NullToInt(_TensDigit.ToString() + _UnitsDigit.ToString());

                    if (_Tmp < 50)
                    {
                        var _DifferenceValue = 50 - _Tmp;
                        _Fan.AirVolume = FuncStr.NullToInt(e.Value) + _DifferenceValue;
                        //_Fan.AirVolume = FuncStr.NullToInt(FuncStr.NullToStr(e.Value).Replace(FuncStr.NullToStr(_Tmp), "50"));
                    }

                    else
                    {
                        var _DifferenceValue = 100 - _Tmp;
                        _Fan.AirVolume = FuncStr.NullToInt(e.Value) + _DifferenceValue;
                    }
                }


                SetFanModel();
            }

            if (e.Column.FieldName == "VentLev" || e.Column.FieldName == "EleLev" || e.Column.FieldName == "MotorTempo" || e.Column.FieldName == "VentNum")
            {
                SetFanModel();
            }


        }

        public ThFanSelectionAxialModelPicker PickThFanSelectionAxialModel(FanDataModel fanmodel, List<AxialFanParameters> lowaxialfanparameters = null)
        {
            if (fanmodel.IsHighSpeedModel())
            {
                if (fanmodel.Control == "双速")
                {
                    var picker = new ThFanSelectionAxialModelPicker(m_ListAxialFanParametersDouble, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
                    //if (!picker.IsFound())
                    //{
                    //    picker = new ThFanSelectionAxialModelPicker(m_ListAxialFanParameters, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
                    //    if (!picker.IsFound())
                    //    {
                    //        fanmodel.Control = "单速";
                    //    }
                    //}
                    return picker;
                }
                else
                {
                    return new ThFanSelectionAxialModelPicker(m_ListAxialFanParameters, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
                }
            }
            else
            {
                return new ThFanSelectionAxialModelPicker(lowaxialfanparameters, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
            }
        }

        public AxialFanParameters FindAxialPickParameters(FanDataModel fanmodel, IFanSelectionModelPicker picker, List<AxialFanParameters> lowaxialfanparameters = null)
        {
            if (fanmodel.IsHighSpeedModel())
            {
                if (fanmodel.Control == "单速")
                {
                    return m_ListAxialFanParameters.Find(p => p.ModelNum == picker.Model() && Convert.ToDouble(p.AirVolume) == picker.AirVolume() && Convert.ToDouble(p.Pa) == picker.Pa());
                }
                else
                {
                    return m_ListAxialFanParametersDouble.Find(p => p.ModelNum == picker.Model() && Convert.ToDouble(p.AirVolume) == picker.AirVolume() && Convert.ToDouble(p.Pa) == picker.Pa());
                }
            }
            else
            {
                return lowaxialfanparameters.Find(p => p.ModelNum == picker.Model() && Convert.ToDouble(p.AirVolume) == picker.AirVolume() && Convert.ToDouble(p.Pa) == picker.Pa());
            }
        }

        public void SetAxialFanDataModel(FanDataModel setmodel, AxialFanParameters origindatamodel, IFanSelectionModelPicker picker)
        {
            setmodel.FanModelID = origindatamodel.No;
            if (setmodel.IsHighSpeedModel())
            {
                setmodel.FanModelName = origindatamodel.ModelNum;
            }
            else
            {
                setmodel.FanModelName = string.Empty;
            }
            setmodel.FanModelNum = origindatamodel.No;
            setmodel.FanModelCCCF = origindatamodel.ModelNum;
            setmodel.FanModelAirVolume = origindatamodel.AirVolume;
            setmodel.FanModelPa = origindatamodel.Pa;
            setmodel.FanModelMotorPower = origindatamodel.Power;
            setmodel.FanModelNoise = origindatamodel.Noise;
            setmodel.FanModelFanSpeed = origindatamodel.Rpm;
            setmodel.FanModelPower = string.Empty;
            setmodel.FanModelLength = origindatamodel.Length;
            setmodel.FanModelDIA = origindatamodel.Diameter;
            setmodel.FanModelWeight = origindatamodel.Weight;
            setmodel.IsPointSafe = !picker.IsOptimalModel();

            m_fmFanModel.CalcFanEfficiency(setmodel);
        }

        public void SetAxialSelectionStateInfo(FanDataModel parentfanmodel, FanDataModel lowfanmodel, ThFanSelectionAxialModelPicker lowfanpick, ThFanSelectionAxialModelPicker parentpick, List<AxialFanParameters> lowaxialfanparameters)
        {
            if (lowfanmodel.FanSelectionStateInfo.IsNull())
            {
                lowfanmodel.FanSelectionStateInfo = new FanSelectionStateInfo();
            }
            else if (parentfanmodel.FanSelectionStateInfo.IsNull())
            {
                parentfanmodel.FanSelectionStateInfo = new FanSelectionStateInfo();
            }

            //高速档未选到风机
            if (string.IsNullOrEmpty(parentfanmodel.FanModelCCCF))
            {
                lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighNotFound;
                parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighNotFound;
            }

            //高速档选到风机且高速档为双速
            else if (parentfanmodel.Control == "双速")
            {
                if (lowfanpick != null && lowfanpick.IsFound())
                {
                    //低速档风机选型点处于安全范围
                    if (!lowfanmodel.IsPointSafe)
                    {
                        //高速档风机选型点处于安全范围
                        if (!parentfanmodel.IsPointSafe)
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothSafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothSafe;
                        }
                        //高速档风机选型点处于危险范围
                        else
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighUnsafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighUnsafe;
                        }
                    }
                    //低速档风机选型点处于危险范围
                    else
                    {
                        //高速档风机选型点处于安全范围
                        if (!parentfanmodel.IsPointSafe)
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowUnsafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowUnsafe;
                        }
                        //高速档风机选型点处于危险范围
                        else
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothUnsafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothUnsafe;
                        }
                    }
                }

                //低速档风机未选到
                else
                {
                    var lowgeometry = lowaxialfanparameters.ToGeometries(new AxialModelNumberComparer(), "高");

                    if (parentpick.IsFound())
                    {
                        var highreferencepoint = parentpick.ModelGeometry().ReferenceModelPoint(new List<double>() { parentfanmodel.AirVolume, parentfanmodel.WindResis }, lowgeometry.First());
                        List<double> recommendPointInLow = new List<double> { 0, 0 };
                        if (highreferencepoint.Count != 0)
                        {
                            recommendPointInLow = new List<double> { Math.Round(highreferencepoint.First().X), Math.Round(highreferencepoint.First().Y) };
                        }
                        lowfanmodel.FanSelectionStateInfo.RecommendPointInLow = recommendPointInLow;
                        parentfanmodel.FanSelectionStateInfo.RecommendPointInLow = recommendPointInLow;
                    }
                    lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowNotFound;
                    parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowNotFound;

                    ClearFanModel(lowfanmodel);
                }
            }
        }

        public ThFanSelectionModelPicker PickThFanSelectionFugeModel(FanDataModel fanmodel, List<FanParameters> lowfugefanparameters = null)
        {
            if (fanmodel.IsHighSpeedModel())
            {
                if (FuncStr.NullToStr(fanmodel.VentStyle).Contains("前倾离心"))
                {
                    if (fanmodel.Control == "双速")
                    {
                        var picker = new ThFanSelectionModelPicker(m_ListFanParametersDouble, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
                        //if (!picker.IsFound())
                        //{
                        //    picker = new ThFanSelectionModelPicker(m_ListFanParameters, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
                        //    if (!picker.IsFound())
                        //    {
                        //        fanmodel.Control = "单速";
                        //    }
                        //}
                        return picker;
                    }
                    else
                    {
                        return new ThFanSelectionModelPicker(m_ListFanParameters, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
                    }

                }
                else
                {

                    if (fanmodel.Control == "双速")
                    {
                        ClearFanModel(fanmodel);
                        return null;
                    }
                    else
                    {
                        return new ThFanSelectionModelPicker(m_ListFanParametersSingle, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
                    }
                }
            }
            else
            {
                return new ThFanSelectionModelPicker(lowfugefanparameters, fanmodel, new List<double>() { fanmodel.GetAirVolume(), fanmodel.WindResis, 0 });
            }
        }

        public void SetFugeFanDataModel(FanDataModel setmodel, FanParameters origindatamodel, IFanSelectionModelPicker picker)
        {
            setmodel.FanModelID = origindatamodel.Suffix;
            if (setmodel.IsHighSpeedModel())
            {
                setmodel.FanModelName = origindatamodel.CCCF_Spec;
            }
            else
            {
                setmodel.FanModelName = string.Empty;
            }
            setmodel.FanModelNum = origindatamodel.No;
            setmodel.FanModelCCCF = origindatamodel.CCCF_Spec;
            setmodel.FanModelAirVolume = origindatamodel.AirVolume;
            setmodel.FanModelPa = origindatamodel.Pa;
            setmodel.FanModelMotorPower = origindatamodel.Power;
            setmodel.FanModelNoise = origindatamodel.Noise;
            setmodel.FanModelFanSpeed = origindatamodel.Rpm;
            setmodel.FanModelPower = string.Empty;
            setmodel.FanModelLength = origindatamodel.Length;
            setmodel.FanModelWidth = origindatamodel.Width;
            setmodel.FanModelHeight = origindatamodel.Height;
            setmodel.FanModelWeight = origindatamodel.Weight;
            setmodel.IsPointSafe = !picker.IsOptimalModel();
            if (setmodel.VentStyle.Contains("电机内置"))
            {
                setmodel.FanModelLength = origindatamodel.Length2;
                setmodel.FanModelWidth = origindatamodel.Width1;
                setmodel.FanModelHeight = origindatamodel.Height2;
            }

            m_fmFanModel.CalcFanEfficiency(setmodel);
        }

        public void SetFugeSelectionStateInfo(FanDataModel parentfanmodel, FanDataModel lowfanmodel, ThFanSelectionModelPicker lowfanpick, ThFanSelectionModelPicker parentpick, List<FanParameters> lowfugefanparameters)
        {
            if (lowfanmodel.FanSelectionStateInfo.IsNull())
            {
                lowfanmodel.FanSelectionStateInfo = new FanSelectionStateInfo();
            }
            else if (parentfanmodel.FanSelectionStateInfo.IsNull())
            {
                parentfanmodel.FanSelectionStateInfo = new FanSelectionStateInfo();
            }

            //高速档未选到风机
            if (string.IsNullOrEmpty(parentfanmodel.FanModelCCCF))
            {
                lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighNotFound;
                parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighNotFound;
            }

            //高速档选到风机且高速档为双速
            else if (parentfanmodel.Control == "双速")
            {
                if (lowfanpick != null && lowfanpick.IsFound())
                {
                    //低速档风机选型点处于安全范围
                    if (!lowfanmodel.IsPointSafe)
                    {
                        //高速档风机选型点处于安全范围
                        if (!parentfanmodel.IsPointSafe)
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothSafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothSafe;
                        }
                        //高速档风机选型点处于危险范围
                        else
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighUnsafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighUnsafe;
                        }
                    }
                    //低速档风机选型点处于危险范围
                    else
                    {
                        //高速档风机选型点处于安全范围
                        if (!parentfanmodel.IsPointSafe)
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowUnsafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowUnsafe;
                        }
                        //高速档风机选型点处于危险范围
                        else
                        {
                            lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothUnsafe;
                            parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothUnsafe;
                        }
                    }
                }

                //低速档风机未选到
                else
                {
                    var lowgeometry = lowfugefanparameters.ToGeometries(new CCCFComparer(), "高");

                    if (parentpick.IsFound())
                    {
                        var highreferencepoint = parentpick.ModelGeometry().ReferenceModelPoint(new List<double>() { parentfanmodel.AirVolume, parentfanmodel.WindResis }, lowgeometry.First());
                        List<double> recommendPointInLow = new List<double> { 0, 0 };
                        if (highreferencepoint.Count != 0)
                        {
                            recommendPointInLow = new List<double> { Math.Round(highreferencepoint.First().X), Math.Round(highreferencepoint.First().Y) };
                        }
                        lowfanmodel.FanSelectionStateInfo.RecommendPointInLow = recommendPointInLow;
                        parentfanmodel.FanSelectionStateInfo.RecommendPointInLow = recommendPointInLow;
                    }
                    lowfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowNotFound;
                    parentfanmodel.FanSelectionStateInfo.fanSelectionState = FanSelectionState.LowNotFound;

                    ClearFanModel(lowfanmodel);
                }
            }
        }

        public void SetFanModel()
        {
            if (TreeList == null)
            { return; }
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null)
            { return; }
            _Fan.FanSelectionStateInfo = new FanSelectionStateInfo();
            //if (_Fan.AirVolume == 0 || _Fan.WindResis == 0)
            //{
            //    ClearFanModel(_Fan);
            //    if (m_ListFan.HasChildModel(_Fan))
            //    {
            //        ClearFanModel(m_ListFan.ChildModel(_Fan));
            //    }
            //    return;
            //}
            if (FuncStr.NullToStr(_Fan.AirVolume) == string.Empty || FuncStr.NullToStr(_Fan.VentStyle) == string.Empty || FuncStr.NullToStr(_Fan.WindResis) == string.Empty)
            {
                ClearFanModel(_Fan);
                return;
            }

            if (_Fan.IsHighSpeedModel())
            {
                // 高速轴流
                if (_Fan.IsAXIALModel())
                {
                    List<AxialFanParameters> _ListAxialFanParameters = new List<AxialFanParameters>();
                    IFanSelectionModelPicker picker = PickThFanSelectionAxialModel(_Fan);

                    if (picker != null && picker.IsFound())
                    {
                        var _FanParameters = FindAxialPickParameters(_Fan, picker);
                        if (_FanParameters != null)
                        {
                            SetAxialFanDataModel(_Fan, _FanParameters, picker);
                            if (!_Fan.IsPointSafe)
                            {
                                _Fan.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothSafe;
                            }
                            else
                            {
                                _Fan.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighUnsafe;
                            }

                        }
                        m_fmFanModel.InitForm(_Fan, m_ListFan);
                    }
                    else
                    {
                        _Fan.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighNotFound;
                        ClearFanModel(_Fan);
                    }
                    TreeList.RefreshNode(TreeList.FocusedNode);
                }

                // 高速离心
                else
                {
                    List<FanParameters> _ListFanParameters = new List<FanParameters>();
                    IFanSelectionModelPicker picker = null;
                    if (FuncStr.NullToStr(_Fan.VentStyle).Contains("前倾离心"))
                    {
                        if (_Fan.Control == "双速")
                        {
                            picker = new ThFanSelectionModelPicker(m_ListFanParametersDouble, _Fan, new List<double>() { _Fan.GetAirVolume(), _Fan.WindResis, 0 });
                            _ListFanParameters = m_ListFanParametersDouble;
                            //if (!picker.IsFound())
                            //{
                            //    picker = new ThFanSelectionModelPicker(m_ListFanParameters, _Fan, new List<double>() { _Fan.GetAirVolume(), _Fan.WindResis, 0 });
                            //    if (!picker.IsFound())
                            //    {
                            //        _Fan.Control = "单速";
                            //        _ListFanParameters = m_ListFanParameters;
                            //    }

                            //}
                        }
                        else
                        {
                            picker = new ThFanSelectionModelPicker(m_ListFanParameters, _Fan, new List<double>() { _Fan.GetAirVolume(), _Fan.WindResis, 0 });
                            _ListFanParameters = m_ListFanParameters;
                        }

                    }
                    else
                    {

                        if (_Fan.Control == "双速")
                        {
                            ClearFanModel(_Fan);
                        }
                        else
                        {
                            picker = new ThFanSelectionModelPicker(m_ListFanParametersSingle, _Fan, new List<double>() { _Fan.GetAirVolume(), _Fan.WindResis, 0 });
                            _ListFanParameters = m_ListFanParametersSingle;
                        }
                    }

                    if (picker != null && picker.IsFound())
                    {
                        var _FanParameters = _ListFanParameters.Find(p => p.CCCF_Spec == picker.Model() && Convert.ToDouble(p.AirVolume) == picker.AirVolume() && Convert.ToDouble(p.Pa) == picker.Pa());
                        if (_FanParameters != null)
                        {
                            SetFugeFanDataModel(_Fan, _FanParameters, picker);
                            if (!_Fan.IsPointSafe)
                            {
                                _Fan.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighAndLowBothSafe;
                            }
                            else
                            {
                                _Fan.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighUnsafe;
                            }

                            m_fmFanModel.InitForm(_Fan, m_ListFan);
                        }

                    }
                    else
                    {
                        _Fan.FanSelectionStateInfo.fanSelectionState = FanSelectionState.HighNotFound;
                        ClearFanModel(_Fan);
                    }

                    TreeList.RefreshNode(TreeList.FocusedNode);
                }

                if (m_ListFan.HasChildModel(_Fan))
                {
                    _Fan = m_ListFan.ChildModel(_Fan);
                    _Fan.FanSelectionStateInfo = new FanSelectionStateInfo();
                }
            }
            if (!_Fan.IsHighSpeedModel())
            {
                var parent = m_ListFan.ParentModel(_Fan);
                if (FuncStr.NullToStr(parent.VentStyle).Contains("前倾离心"))
                {
                    List<FanParameters> _ListFanParameters = new List<FanParameters>();
                    //高速档父项为前倾离心双速
                    if (parent.Control == "双速")
                    {
                        _ListFanParameters = m_ListFanParametersDouble.Where(f => f.CCCF_Spec == parent.FanModelCCCF).ToList();
                    }
                    //高速档父项为前倾离心单速
                    else
                    {
                        _ListFanParameters = m_ListFanParameters.Where(f => f.CCCF_Spec == parent.FanModelCCCF).ToList();
                    }
                    ThFanSelectionModelPicker picker = null;
                    //若当前节点为低速档
                    //先找到他的高速档父节点，并获取父节点选出的风机CCCF
                    //用高速档选型风机的CCCF过滤源数据，从而获取用于低速档选型的曲线(即父节点选型曲线对应的低速档曲线)
                    //var parentfan = m_ListFan.Find(p => p.ID == _Fan.PID);

                    picker = new ThFanSelectionModelPicker(_ListFanParameters, _Fan, new List<double>() { _Fan.GetAirVolume(), _Fan.WindResis, 0 });
                    if (picker != null)
                    {
                        if (picker.IsFound())
                        {
                            var _FanParameters = _ListFanParameters.Find(p => p.CCCF_Spec == picker.Model() && Convert.ToDouble(p.AirVolume) == picker.AirVolume() && Convert.ToDouble(p.Pa) == picker.Pa());
                            if (_FanParameters != null)
                            {
                                SetFugeFanDataModel(_Fan, _FanParameters, picker);
                            }

                        }
                        m_fmFanModel.InitForm(_Fan, m_ListFan);
                    }

                    var parentpick = new ThFanSelectionModelPicker(m_ListFanParametersDouble, parent, new List<double>() { parent.GetAirVolume(), parent.WindResis, 0 });
                    SetFugeSelectionStateInfo(parent, _Fan, picker, parentpick, _ListFanParameters);
                    TreeList.RefreshNode(TreeList.FocusedNode);
                }
                else if (FuncStr.NullToStr(parent.VentStyle).Contains("后倾离心"))
                {
                    //高速档父项为后倾离心双速
                    if (parent.Control == "双速")
                    {
                        ClearFanModel(_Fan);
                        TreeList.RefreshNode(TreeList.FocusedNode);
                        return;
                    }
                    //高速档父项为后倾离心单速
                    else
                    {
                        var _ListFanParameters = m_ListFanParametersSingle.Where(f => f.CCCF_Spec == parent.FanModelCCCF).ToList();
                        ThFanSelectionModelPicker picker = null;
                        //若当前节点为低速档
                        //先找到他的高速档父节点，并获取父节点选出的风机CCCF
                        //用高速档选型风机的CCCF过滤源数据，从而获取用于低速档选型的曲线(即父节点选型曲线对应的低速档曲线)
                        //var parentfan = m_ListFan.Find(p => p.ID == _Fan.PID);

                        picker = new ThFanSelectionModelPicker(_ListFanParameters, _Fan, new List<double>() { _Fan.GetAirVolume(), _Fan.WindResis, 0 });
                        if (picker != null)
                        {
                            if (picker.IsFound())
                            {
                                var _FanParameters = _ListFanParameters.Find(p => p.CCCF_Spec == picker.Model() && Convert.ToDouble(p.AirVolume) == picker.AirVolume() && Convert.ToDouble(p.Pa) == picker.Pa());
                                if (_FanParameters != null)
                                {
                                    SetFugeFanDataModel(_Fan, _FanParameters, picker);
                                }

                            }
                            m_fmFanModel.InitForm(_Fan, m_ListFan);
                        }

                        var parentpick = new ThFanSelectionModelPicker(m_ListFanParametersDouble, parent, new List<double>() { parent.GetAirVolume(), parent.WindResis, 0 });
                        SetFugeSelectionStateInfo(parent, _Fan, picker, parentpick, _ListFanParameters);
                        TreeList.RefreshNode(TreeList.FocusedNode);
                    }
                }
                else
                {
                    List<AxialFanParameters> _ListAxialFanParameters = new List<AxialFanParameters>();
                    if (parent.Control == "双速")
                    {
                        _ListAxialFanParameters = m_ListAxialFanParametersDouble.Where(f => f.ModelNum == parent.FanModelCCCF).ToList();
                    }
                    else
                    {
                        _ListAxialFanParameters = m_ListAxialFanParameters.Where(f => f.ModelNum == parent.FanModelCCCF).ToList();
                    }
                    ThFanSelectionAxialModelPicker picker = null;
                    //若当前节点为低速档
                    //先找到他的高速档父节点，并获取高速档选出的风机CCCF
                    //用高速档选型风机的CCCF过滤源数据，从而获取用于低速档选型的曲线(即父节点选型曲线对应的低速档曲线)
                    picker = new ThFanSelectionAxialModelPicker(_ListAxialFanParameters, _Fan, new List<double>() { _Fan.GetAirVolume(), _Fan.WindResis, 0 });
                    if (picker != null)
                    {
                        if (picker.IsFound())
                        {
                            var _FanParameters = FindAxialPickParameters(_Fan, picker, _ListAxialFanParameters);
                            if (_FanParameters != null)
                            {
                                SetAxialFanDataModel(_Fan, _FanParameters, picker);
                            }
                        }
                        m_fmFanModel.InitForm(_Fan, m_ListFan);
                    }

                    var parentpick = PickThFanSelectionAxialModel(parent);
                    SetAxialSelectionStateInfo(parent, _Fan, picker, parentpick, _ListAxialFanParameters);
                    TreeList.RefreshNode(TreeList.FocusedNode);
                }
            }
        }

        private static void ClearFanModel(FanDataModel _Fan)
        {
            _Fan.FanModelID = string.Empty;
            _Fan.FanModelName = string.Empty;
            _Fan.FanModelNum = string.Empty;
            _Fan.FanModelCCCF = string.Empty;
            _Fan.FanModelAirVolume = string.Empty;
            _Fan.FanModelPa = string.Empty;
            _Fan.FanModelMotorPower = string.Empty;
            _Fan.FanModelNoise = string.Empty;
            _Fan.FanModelFanSpeed = string.Empty;
            _Fan.FanModelPower = string.Empty;
            _Fan.FanModelLength = string.Empty;
            _Fan.FanModelWidth = string.Empty;
            _Fan.FanModelHeight = string.Empty;
            _Fan.FanModelWeight = string.Empty;
        }

        private void TreeList_CustomNodeCellEditForEditing(object sender, GetCustomNodeCellEditEventArgs e)
        {
            var _TreeList = sender as TreeList;
            if (_TreeList == null) { return; }
            var _Fan = _TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }
            if (e.Column.FieldName == "VentConnect")
            {

                var _Edit = TreeList.RepositoryItems["ComBoxVentConnect"] as DevExpress.XtraEditors.Repository.RepositoryItemComboBox;


                if (FuncStr.NullToStr(_Fan.VentStyle) == "轴流")
                {
                    _Edit.Items.Clear();
                    ComBoxVentConnect.Items.Add("直连");
                }
                else if (FuncStr.NullToStr(_Fan.VentStyle).Contains("电机外置"))
                {
                    _Edit.Items.Clear();
                    ComBoxVentConnect.Items.Add("皮带");
                }
                else
                {
                    _Edit.Items.Clear();
                    ComBoxVentConnect.Items.Add("皮带");
                }

                e.RepositoryItem = _Edit;
            }

            if (e.Column.FieldName == "IntakeForm")
            {

                var _Edit = TreeList.RepositoryItems["ComBoxIntakeForm"] as DevExpress.XtraEditors.Repository.RepositoryItemComboBox;


                if (FuncStr.NullToStr(_Fan.VentStyle) == "轴流")
                {
                    _Edit.Items.Clear();
                    _Edit.Items.Add("直进直出");
                }
                else
                {
                    _Edit.Items.Clear();
                    _Edit.Items.Add("直进直出");
                    _Edit.Items.Add("直进上出");
                    _Edit.Items.Add("直进下出");
                    _Edit.Items.Add("侧进直出");
                    _Edit.Items.Add("上进直出");
                    _Edit.Items.Add("下进直出");
                }

                e.RepositoryItem = _Edit;
            }

            if (e.Column.FieldName == "Use")
            {

                if (FuncStr.NullToStr(_Fan.Scenario) == "消防排烟兼平时排风" || FuncStr.NullToStr(_Fan.Scenario) == "消防补风兼平时送风")
                {
                    var _EditTxt = TreeList.RepositoryItems["TxtUse"] as DevExpress.XtraEditors.Repository.RepositoryItemTextEdit;

                    e.RepositoryItem = _EditTxt;
                }

                if (FuncStr.NullToStr(_Fan.Scenario) == "平时排风兼事故排风" || FuncStr.NullToStr(_Fan.Scenario) == "平时送风兼事故补风")
                {
                    var _EditComBox = TreeList.RepositoryItems["ComBoxUse"] as DevExpress.XtraEditors.Repository.RepositoryItemComboBox;


                    e.RepositoryItem = _EditComBox;
                }




            }


            if (e.Column.FieldName == "VibrationMode")
            {

                var _Edit = TreeList.RepositoryItems["ComBoxVibrationMode"] as DevExpress.XtraEditors.Repository.RepositoryItemComboBox;


                if (FuncStr.NullToStr(_Fan.Scenario).Contains("消防"))
                {
                    _Edit.Items.Clear();
                    _Edit.Items.Add("-");
                    _Edit.Items.Add("S");
                }
                else
                {
                    _Edit.Items.Clear();
                    _Edit.Items.Add("-");
                    _Edit.Items.Add("R");
                    _Edit.Items.Add("S");
                }
            }



        }

        public string ReadTxt(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = File.OpenText(_Path))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                XtraMessageBox.Show("数据文件读取时发生错误！");
                return string.Empty;

            }
        }

        private void ComBoxScene_SelectedValueChanged(object sender, EventArgs e)
        {
            switch (FuncStr.NullToStr(ComBoxScene.EditValue))
            {
                case "平时送风":
                case "平时排风":
                    ColAddAuxiliary.Visible = true;
                    BandUse.Visible = false;
                    break;
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                case "平时排风兼事故排风":
                case "平时送风兼事故补风":
                    BandUse.Visible = true;
                    ColAddAuxiliary.Visible = false;
                    break;
                default:
                    BandUse.Visible = false;
                    ColAddAuxiliary.Visible = false;
                    break;
            }


            var _FilterString = @" Scenario =  '" + FuncStr.NullToStr(ComBoxScene.EditValue) + "'";

            _FilterString += @" AND IsErased = false ";

            try
            {
                TreeList.ActiveFilterString = _FilterString;
            }
            catch
            {


            }




            if (m_ListFan == null) { m_ListFan = new List<FanDataModel>(); }

            if (m_ListFan == null || m_ListFan.Count == 0)
            {
                BtnAdd_Click(null, null);
                return;
            }

            var _List = m_ListFan.FindAll(p => p.Scenario == FuncStr.NullToStr(ComBoxScene.EditValue));

            if (_List == null || _List.Count == 0)
            {
                BtnAdd_Click(null, null);
                return;
            }
        }

        private void PictAddAuxiliary_Click(object sender, EventArgs e)
        {
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null || _Fan.PID != "0") { return; }
            var _ListFan = m_ListFan.FindAll(p => p.PID == _Fan.ID && p.ID != _Fan.ID);
            if (_ListFan == null || _ListFan.Count == 0)
            {
                FanDataModel _FanDataModel = new FanDataModel();
                _FanDataModel.ID = Guid.NewGuid().ToString();
                _FanDataModel.Scenario = FuncStr.NullToStr(ComBoxScene.EditValue);
                _FanDataModel.PID = _Fan.ID;
                _FanDataModel.Name = "低速";
                _FanDataModel.AirVolume = 0;

                _FanDataModel.InstallSpace = "-";
                _FanDataModel.InstallFloor = "-";
                _FanDataModel.VentQuan = 0;
                _FanDataModel.VentNum = "-";

                _FanDataModel.VentStyle = "-";
                _FanDataModel.VentConnect = "-";
                _FanDataModel.VentLev = "-";
                _FanDataModel.EleLev = "-";
                _FanDataModel.FanModelName = "-";
                _FanDataModel.VibrationMode = "-";
                _FanDataModel.MountType = "-";
                m_ListFan.Add(_FanDataModel);
            }
            _Fan.Control = "双速";
            SetFanModel();
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();
        }

        private void TreeList_ShowingEditor(object sender, CancelEventArgs e)
        {
            var _TreeList = sender as TreeList;
            if (_TreeList == null) { return; }

            var _FanDataModel = _TreeList.GetFocusedRow() as FanDataModel;
            if (_FanDataModel == null) { return; }
            if (_FanDataModel.PID != "0")
            {
                if (_TreeList.FocusedColumn.FieldName != "SysAirVolume" && _TreeList.FocusedColumn.FieldName != "WindResis")
                {
                    e.Cancel = true;
                    return;
                }
            }


            //if (_TreeList.FocusedColumn.FieldName == "FanModelName")
            //{
            //    if (_FanDataModel.FanModelName == string.Empty || _FanDataModel.FanModelName == "无此风机")
            //    {
            //        e.Cancel = true;
            //        return;
            //    }

            //}



            if (_TreeList.FocusedColumn.FieldName == "Use")
            {
                if (FuncStr.NullToStr(_FanDataModel.Scenario) == "消防排烟兼平时排风" || FuncStr.NullToStr(_FanDataModel.Scenario) == "消防补风兼平时送风")
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (_TreeList.FocusedColumn.FieldName == "IntakeForm")
            {
                if (FuncStr.NullToStr(_FanDataModel.VentStyle) == "轴流")
                {
                    e.Cancel = true;
                    return;

                }

            }



        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {

            TreeList.PostEditor();
            BtnAdd.Focus();
            FanDataModel _FanDataModel = new FanDataModel();
            _FanDataModel.Scenario = FuncStr.NullToStr(ComBoxScene.EditValue);
            _FanDataModel.ID = Guid.NewGuid().ToString();
            _FanDataModel.PID = "0";
            _FanDataModel.Name = "未命名风机";
            _FanDataModel.InstallSpace = "未指定子项";
            _FanDataModel.InstallFloor = "未指定楼层";
            _FanDataModel.VentNum = "1";
            _FanDataModel.VentQuan = 1;
            _FanDataModel.ListVentQuan = new List<int>() { 1 };
            _FanDataModel.Remark = string.Empty;
            _FanDataModel.AirVolume = 0;
            _FanDataModel.WindResis = 0;
            _FanDataModel.VentStyle = "前倾离心(电机内置)";
            _FanDataModel.VentConnect = "皮带";
            _FanDataModel.IntakeForm = "直进直出";
            _FanDataModel.VentLev = "2级";
            _FanDataModel.EleLev = "2级";
            _FanDataModel.MotorTempo = 1450;
            _FanDataModel.FanModelName = string.Empty;
            _FanDataModel.MountType = "吊装";
            _FanDataModel.Control = "单速";
            _FanDataModel.PowerType = "普通";
            _FanDataModel.VibrationMode = "S";
            _FanDataModel.SortID = m_ListFan.Count + 1;

            var _FanPrefixDict = PubVar.g_ListFanPrefixDict.Find(s => s.FanUse == _FanDataModel.Scenario);
            if (_FanPrefixDict != null)
            {
                _FanDataModel.SortScenario = _FanPrefixDict.No;
            }

            var scenario = FuncStr.NullToStr(ComBoxScene.EditValue);
            switch (scenario)
            {
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                    {
                        _FanDataModel.Remark = "消防兼用";
                        _FanDataModel.Use = "消防排烟";
                        _FanDataModel.Control = "双速";
                        m_ListFan.Add(_FanDataModel.CreateAuxiliaryModel(scenario));
                    }
                    break;
                case "平时送风兼事故补风":
                case "平时排风兼事故排风":
                    {
                        _FanDataModel.Remark = "事故兼用";
                        _FanDataModel.Use = "事故排风";
                        _FanDataModel.Control = "双速";
                        m_ListFan.Add(_FanDataModel.CreateAuxiliaryModel(scenario));
                    }
                    break;
                default:
                    break;
            }

            if (FuncStr.NullToStr(_FanDataModel.Scenario).Contains("消防"))
            {
                _FanDataModel.PowerType = "消防";
                _FanDataModel.VentStyle = "轴流";
                _FanDataModel.VentConnect = "直连";
                _FanDataModel.IntakeForm = "直进直出";

            }
            if (FuncStr.NullToStr(_FanDataModel.Scenario).Contains("事故"))
            {
                _FanDataModel.PowerType = "事故";
            }
            if (FuncStr.NullToStr(_FanDataModel.Scenario) == "消防加压送风" || FuncStr.NullToStr(_FanDataModel.Scenario) == "消防排烟"
                || FuncStr.NullToStr(_FanDataModel.Scenario) == "消防补风")
            {
                _FanDataModel.VibrationMode = "-";
            }

            m_ListFan.Add(_FanDataModel);
            if (m_ListFan != null && m_ListFan.Count > 0)
                m_ListFan = m_ListFan.OrderBy(p => p.SortID).ToList();
            TreeList.DataSource = m_ListFan;
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();


            TreeList.FocusedNode = TreeList.Nodes.LastNode;
            TreeList.ShowEditor();
        }

        private void BtnDle_Click(object sender, EventArgs e)
        {
            TreeList.PostEditor();
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null || TreeList.FocusedNode == null) { return; }
            if (_Fan.Scenario == "平时送风" || _Fan.Scenario == "平时排风")
            {
                if (_Fan.PID == "0")
                {
                    if (XtraMessageBox.Show(" 已插入图纸的风机图块也将被删除，是否继续？ ", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        _Fan.IsErased = true;
                        //TreeList.DeleteSelectedNodes();
                        var _SonFan = m_ListFan.Find(p => p.PID == _Fan.ID);
                        if (_SonFan != null)
                        {
                            _SonFan.IsErased = true;
                        }

                        using (Active.Document.LockDocument())
                        using (AcadDatabase acadDatabase = AcadDatabase.Active())
                        using (ThHvacDbModelManager dbManager = new ThHvacDbModelManager(Active.Database))
                        {
                            dbManager.EraseModels(_Fan.ID);
                            Active.Editor.Regen();
                        }
                        ComBoxScene_SelectedValueChanged(null, null);
                    }
                }
                else
                {
                    if (XtraMessageBox.Show(" 是否确认删除低速工况？ ", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        //_Fan.IsErased = true;
                        TreeList.DeleteSelectedNodes();
                        SetFanModel();
                        ComBoxScene_SelectedValueChanged(null, null);
                    }
                }

            }
            else
            {
                if (XtraMessageBox.Show(" 已插入图纸的风机图块也将被删除，是否继续？ ", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (_Fan.PID == "0")
                    {
                        _Fan.IsErased = true;
                        //TreeList.DeleteSelectedNodes();

                        var _SonFan = m_ListFan.Find(p => p.PID == _Fan.ID);
                        if (_SonFan != null)
                        {
                            _SonFan.IsErased = true;
                        }

                        using (Active.Document.LockDocument())
                        using (AcadDatabase acadDatabase = AcadDatabase.Active())
                        using (ThHvacDbModelManager dbManager = new ThHvacDbModelManager(Active.Database))
                        {
                            dbManager.EraseModels(_Fan.ID);
                            Active.Editor.Regen();
                        }
                    }
                    else
                    {
                        _Fan.IsErased = true;
                        //TreeList.DeleteSelectedNodes();
                        var _MainFan = m_ListFan.Find(p => p.ID == _Fan.PID);
                        if (_MainFan != null)
                        {
                            _MainFan.IsErased = true;
                            //m_ListFan.Remove(_MainFan);
                            TreeList.RefreshDataSource();
                            this.TreeList.ExpandAll();

                            using (Active.Document.LockDocument())
                            using (AcadDatabase acadDatabase = AcadDatabase.Active())
                            using (ThHvacDbModelManager dbManager = new ThHvacDbModelManager(Active.Database))
                            {
                                dbManager.EraseModels(_MainFan.ID);
                                Active.Editor.Regen();
                            }
                            //SetFanModel();
                        }
                    }

                    TreeList.Refresh();
                    ComBoxScene_SelectedValueChanged(null, null);
                    m_fmOverView.DataSourceChanged(m_ListFan);
                }
            }





            //var _List = TreeList.GetAllCheckedNodes();
            //List<FanDataModel> _ListFan = new List<FanDataModel>();
            //if (_List != null && _List.Count > 0)
            //{
            //    _List.ForEach(p =>
            //   {
            //       var _ID = p.GetValue("ID");
            //       var _Fan = m_ListFan.Find(s => FuncStr.NullToStr(s.ID) == FuncStr.NullToStr(_ID));
            //       if (_Fan != null)
            //           _ListFan.Add(_Fan);
            //   });
            //    if (_ListFan != null && _ListFan.Count > 0)
            //    {
            //        if (XtraMessageBox.Show(" 已插入图纸的风机图块也将被删除，是否继续？ ", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            //        {
            //            m_ListFan.RemoveAll(p => _ListFan.Contains(p));
            //            TreeList.DataSource = m_ListFan;
            //            TreeList.RefreshDataSource();
            //            this.TreeList.ExpandAll();

            //        }
            //    }

            //}
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {


            //TreeListNode _FocuesNode = this.TreeList.FocusedNode;
            //TreeList.SetNodeIndex(_FocuesNode, 0);



            TreeList.PostEditor();
            var _Index = TreeList.GetNodeIndex(TreeList.FocusedNode);
            if (_Index == 0) { return; }
            TreeListNode _FocuesNode = this.TreeList.FocusedNode;
            var _FocusedNodeID = this.TreeList.FocusedNode.Id;
            TreeList.BeginUpdate();
            int PrevNodeIndex = this.TreeList.GetNodeIndex(_FocuesNode.PrevNode);
            TreeList.SetNodeIndex(_FocuesNode, PrevNodeIndex);
            TreeList.EndUpdate();
            for (int i = 0; i < TreeList.Nodes.Count; i++)
            {
                var _ID = TreeList.Nodes[i].GetValue("ID");
                var _Name = TreeList.Nodes[i].GetValue("Name");
                var _iX = TreeList.GetNodeIndex(TreeList.Nodes[i]);
                var _Fan = m_ListFan.Find(p => FuncStr.NullToStr(p.ID) == FuncStr.NullToStr(_ID));
                if (_Fan != null)
                    _Fan.SortID = _iX;

            }
            if (m_ListFan != null && m_ListFan.Count > 0)
                m_ListFan = m_ListFan.OrderBy(p => p.SortID).ToList();
            TreeList.DataSource = m_ListFan;
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();

            TreeList.FocusedNode = TreeList.Nodes.LastNode;
            TreeList.FocusedNode = TreeList.FindNodeByID(_FocusedNodeID - 1);

            m_fmOverView.DataSourceChanged(m_ListFan);


        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            TreeList.PostEditor();
            var _Index = TreeList.GetNodeIndex(TreeList.FocusedNode);
            if (_Index == m_ListFan.Count - 1) { return; }
            TreeList.Columns["SortID"].SortOrder = SortOrder.None;
            TreeListNode _FocuesNode = this.TreeList.FocusedNode;
            var _FocusedNodeID = this.TreeList.FocusedNode.Id;
            TreeList.BeginUpdate();
            int PrevNodeIndex = this.TreeList.GetNodeIndex(_FocuesNode.NextNode);
            TreeList.SetNodeIndex(_FocuesNode, PrevNodeIndex);
            TreeList.EndUpdate();
            for (int i = 0; i < TreeList.Nodes.Count; i++)
            {
                var _ID = TreeList.Nodes[i].GetValue("ID");
                var _Name = TreeList.Nodes[i].GetValue("Name");
                var _iX = TreeList.GetNodeIndex(TreeList.Nodes[i]);
                var _Fan = m_ListFan.Find(p => FuncStr.NullToStr(p.ID) == FuncStr.NullToStr(_ID));
                if (_Fan != null)
                    _Fan.SortID = _iX;

            }
            if (m_ListFan != null && m_ListFan.Count > 0)
                m_ListFan = m_ListFan.OrderBy(p => p.SortID).ToList();
            TreeList.DataSource = m_ListFan;
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();
            TreeList.FocusedNode = TreeList.FindNodeByID(_FocusedNodeID + 1);

            m_fmOverView.DataSourceChanged(m_ListFan);
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null || TreeList.FocusedNode == null) { return; }
            List<FanDataModel> _ListTemp = new List<FanDataModel>();
            string _Guid = Guid.NewGuid().ToString();
            var _Json = FuncJson.Serialize(_Fan);
            var _FanDataModel = FuncJson.Deserialize<FanDataModel>(_Json);
            if (_Fan.PID == "0")
            {
                _FanDataModel.ID = _Guid;
                _FanDataModel.PID = "0";
                //_FanDataModel.Name = SetFanDataModelName(_FanDataModel);
                _FanDataModel.Name = _FanDataModel.Name;
                _FanDataModel.InstallFloor = SetFanDataModelByFloor(_FanDataModel);
                _ListTemp.Add(_FanDataModel);

                var _SonFan = m_ListFan.Find(p => p.PID == _Fan.ID);
                if (_SonFan != null)
                {
                    var _SonJson = FuncJson.Serialize(_SonFan);
                    var _SonFanData = FuncJson.Deserialize<FanDataModel>(_SonJson);

                    _SonFanData.ID = Guid.NewGuid().ToString();
                    _SonFanData.PID = _Guid;
                    _ListTemp.Add(_SonFanData);
                }


                var _Inidex = m_ListFan.IndexOf(_Fan);
                m_ListFan.InsertRange(_Inidex + 1, _ListTemp);



            }
            else
            {
                var _MainFan = m_ListFan.Find(p => p.ID == _FanDataModel.PID);
                if (_MainFan != null)
                {
                    var _MainJson = FuncJson.Serialize(_MainFan);
                    var _MainFanData = FuncJson.Deserialize<FanDataModel>(_MainJson);
                    _MainFanData.ID = _Guid;
                    _MainFanData.PID = "0";
                    //_MainFanData.Name = SetFanDataModelName(_MainFanData);
                    _MainFanData.Name = _MainFanData.Name;
                    _ListTemp.Add(_MainFanData);
                    var _Inidex = m_ListFan.IndexOf(_MainFan);


                    _FanDataModel.ID = Guid.NewGuid().ToString();
                    _FanDataModel.PID = _Guid;
                    _ListTemp.Add(_FanDataModel);
                    m_ListFan.InsertRange(_Inidex + 1, _ListTemp);
                }


            }
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();
            m_fmOverView.DataSourceChanged(m_ListFan);
        }

        public string SetFanDataModelName(FanDataModel _FanDataModel)
        {
            var _List = m_ListFan.FindAll(p => p.Name.Contains(_FanDataModel.Name) && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
            if (_List == null || _List.Count == 0) { return string.Format("{0}-副本", _FanDataModel.Name); }
            for (int i = 1; i < 10000; i++)
            {
                if (i == 1)
                {
                    var name = string.Format("{0}-副本", _FanDataModel.Name);
                    var _ListTemp1 = m_ListFan.FindAll(p => p.Name == name && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
                    if (_ListTemp1 == null || _ListTemp1.Count == 0) { return name; }
                }
                else
                {
                    var name = string.Format("{0}-副本({1})", _FanDataModel.Name, i);
                    var _ListTemp = m_ListFan.FindAll(p => p.Name == name && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
                    if (_ListTemp == null || _ListTemp.Count == 0) { return name; }
                }

            }
            return string.Empty;
        }

        public string SetFanDataModelByFloor(FanDataModel _FanDataModel)
        {
            var _List = m_ListFan.FindAll(p => p.InstallFloor.Contains(_FanDataModel.InstallFloor) && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
            if (_List == null || _List.Count == 0) { return string.Format("{0}-副本", _FanDataModel.InstallFloor); }
            for (int i = 1; i < 10000; i++)
            {
                if (i == 1)
                {
                    var installFloor = string.Format("{0}-副本", _FanDataModel.InstallFloor);
                    var _ListTemp1 = m_ListFan.FindAll(p => p.InstallFloor == installFloor && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
                    if (_ListTemp1 == null || _ListTemp1.Count == 0) { return installFloor; }
                }
                else
                {
                    var installFloor = string.Format("{0}-副本({1})", _FanDataModel.InstallFloor, i);
                    var _ListTemp = m_ListFan.FindAll(p => p.InstallFloor == installFloor && p.PID == _FanDataModel.PID && p.ID != _FanDataModel.ID);
                    if (_ListTemp == null || _ListTemp.Count == 0) { return installFloor; }
                }

            }
            return string.Empty;
        }

        private void ComBoxUse_EditValueChanged(object sender, EventArgs e)
        {
            TreeList.PostEditor();
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null || TreeList.FocusedNode == null) { return; }
            var _SonFan = m_ListFan.Find(p => p.PID == _Fan.ID);
            if (_SonFan != null)
            {
                if (FuncStr.NullToStr(_Fan.Use) == "事故排风")
                {
                    _SonFan.Use = "平时排风";
                }
                else
                {
                    _SonFan.Use = "事故排风";
                }
                TreeList.RefreshDataSource();
            }

        }

        private void BarBtnExportFanPara_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            fmSceneScreening _fmSceneScreening = new fmSceneScreening();

            _fmSceneScreening.Init(m_ListSceneScreening);

            var _Result = _fmSceneScreening.ShowDialog();

            if (_Result == DialogResult.OK)
            {
                m_ListSceneScreening = _fmSceneScreening.m_List;
            }
            else
            {
                return;
            }

            using (var excelpackage = ThFanSelectionUIUtils.CreateModelExportExcelPackage())
            {
                var _Sheet = excelpackage.Workbook.Worksheets[0];

                var _List = GetListExportFanPara();

                if (_List == null || _List.Count == 0) { return; }

                if (m_ListSceneScreening != null && m_ListSceneScreening.Count > 0)
                {
                    _List = _List.FindAll(p => m_ListSceneScreening.Contains(p.Scenario));
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
                    _Sheet.Cells[i, 7].Value = p.StaticPa;
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

        public List<ExportFanParaModel> GetListExportFanPara()
        {
            List<ExportFanParaModel> _List = new List<ExportFanParaModel>();
            m_ListFan.ForEach(p =>
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

               _ExportFanPara.FanEnergyLevel = p.VentLev;
               _ExportFanPara.DriveMode = p.VentConnect;
               _ExportFanPara.ElectricalEnergyLevel = p.EleLev;
               _ExportFanPara.MotorPower = p.FanModelPowerDescribe;
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

        private void BarBtnExportFanCalc_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            TreeList.Refresh();

            fmSceneScreening _fmSceneScreening = new fmSceneScreening();

            _fmSceneScreening.Init(m_ListSceneScreening);

            var _Result = _fmSceneScreening.ShowDialog();

            if (_Result == System.Windows.Forms.DialogResult.OK)
            {
                m_ListSceneScreening = _fmSceneScreening.m_List;
            }
            else
            {
                return;
            }

            using (var Targetpackage = ThFanSelectionUIUtils.CreateModelCalculateExcelPackage())
            using (var FanVolumeSourcepackage = ThFanSelectionUIUtils.CreateSmokeProofExcelPackage())
            using (var ExhaustSourcepackage = ThFanSelectionUIUtils.CreateSmokeDischargeExcelPackage())
            {
                var _Sheet = Targetpackage.Workbook.Worksheets[0];
                var _List = m_ListFan;

                if (m_ListSceneScreening != null && m_ListSceneScreening.Count > 0)
                {
                    _List = _List.FindAll(p => m_ListSceneScreening.Contains(p.Scenario) && !p.IsErased);
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

                    //if (FuncStr.NullToStr(p.VentStyle) == "轴流")
                    //{
                    //    List<AxialFanParameters> _ListAxialFanParameters = GetAxialFanParametersByControl(p);
                    //    var _FanParameters = _ListAxialFanParameters.Find(s => s.No == FuncStr.NullToStr(p.FanModelID) && s.ModelNum == p.FanModelName);
                    //    if (_FanParameters == null) return;
                    //    _Sheet.Cells[i, 22] = _FanParameters.Pa;
                    //}
                    //else
                    //{
                    //    List<FanParameters> _ListFanParameters = GetFanParametersByControl(p);
                    //    var _FanParameters = _ListFanParameters.Find(s => s.Suffix == FuncStr.NullToStr(p.FanModelID) && s.CCCF_Spec == p.FanModelName);
                    //    if (_FanParameters == null) return;
                    //    _Sheet.Cells[i, 22] = _FanParameters.Pa;
                    //}

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

                            if (_SonFan.IsManualInputAirVolume)
                            {
                                _Sheet.Cells[i, 13].Value = "-";
                            }
                            else
                            {
                                _Sheet.Cells[i, 13].Value = _SonFan.AirCalcValue;
                            }




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

                    var model = p.FanVolumeModel;
                    if (!p.IsNull())
                    {
                        ExcelExportEngine.Instance.Model = p;

                        if (p.FanVolumeModel != null && p.IsManualInputAirVolume != true)
                        {
                            ExcelExportEngine.Instance.RangeCopyOperator = copyOperatorForVolumeModel;
                            ExcelExportEngine.Instance.Sourcebook = FanVolumeSourcepackage.Workbook;
                            ExcelExportEngine.Instance.Targetsheet = Targetpackage.Workbook.Worksheets["防烟计算"];
                            ExcelExportEngine.Instance.Run();
                        }
                        else if (p.ExhaustModel != null && p.IsManualInputAirVolume != true)
                        {
                            ExcelExportEngine.Instance.RangeCopyOperator = copyOperatorForExhaustModel;
                            ExcelExportEngine.Instance.Sourcebook = ExhaustSourcepackage.Workbook;
                            ExcelExportEngine.Instance.Targetsheet = Targetpackage.Workbook.Worksheets["排烟计算"];
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

                    Targetpackage.SaveAs(new FileInfo(_FilePath));
                }
            }
        }


        public int FindNum(int _Num, int _N)
        {
            int _Power = (int)Math.Pow(10, _N);
            return (_Num - _Num / _Power * _Power) * 10 / _Power;
        }

        private void ComBoxVentStyle_EditValueChanged(object sender, EventArgs e)
        {
            TreeList.PostEditor();
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }
            if (FuncStr.NullToStr(_Fan.VentStyle) == "轴流")
            {
                _Fan.VentConnect = "直连";
                _Fan.IntakeForm = "直进直出";
                TreeList.Refresh();
            }
            else if (FuncStr.NullToStr(_Fan.VentStyle).Contains("电机外置"))
            {
                _Fan.VentConnect = "皮带";
                TreeList.Refresh();
            }
            else
            {
                _Fan.VentConnect = "皮带";
                TreeList.Refresh();
            }
            SetFanModel();
        }

        private void PicInsertMap_Click(object sender, EventArgs e)
        {
            var _FocusedColumn = TreeList.FocusedColumn;
            var _FanDataModel = TreeList.GetFocusedRow() as FanDataModel;
            if (_FanDataModel == null)
            {
                return;
            }
            if (!_FanDataModel.IsValid())
            {
                return;
            }
            if (string.IsNullOrEmpty(_FanDataModel.VentStyle))
            {
                return;
            }
            if (_FanDataModel.FanSelectionStateInfo != null && _FanDataModel.FanSelectionStateInfo.fanSelectionState == FanSelectionState.LowNotFound &&
             _FanDataModel.FanSelectionStateInfo.RecommendPointInLow != null && _FanDataModel.FanSelectionStateInfo.RecommendPointInLow.Count == 2)
            {
                XtraMessageBox.Show(string.Format(" 低速挡的工况点与高速挡差异过大,低速档风量的推荐值在{0}m³/h左右,总阻力的推荐值小于{1}Pa. ",
                    _FanDataModel.FanSelectionStateInfo.RecommendPointInLow[0], _FanDataModel.FanSelectionStateInfo.RecommendPointInLow[1]
                    ), "警告", MessageBoxButtons.OK);
                return;
            }

            m_fmFanModel.InitForm(_FanDataModel, m_ListFan);

            // 发送CAD命令
            ThFanSelectionService.Instance.Model = _FanDataModel;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THFJSYSTEMINSERT");
        }

        private void TreeList_CustomDrawNodeCell(object sender, CustomDrawNodeCellEventArgs e)
        {
            FanDataModel _Fan = TreeList.GetDataRecordByNode(e.Node) as FanDataModel;
            if (_Fan == null) return;
            if (e.Column.FieldName == "FanModelName")
            {
                if (_Fan.FanSelectionStateInfo != null)
                {
                    if (_Fan.FanSelectionStateInfo.fanSelectionState == FanSelectionState.HighUnsafe
                        || _Fan.FanSelectionStateInfo.fanSelectionState == FanSelectionState.LowUnsafe
                        || _Fan.FanSelectionStateInfo.fanSelectionState == FanSelectionState.HighAndLowBothUnsafe)
                    {
                        e.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
                        e.Appearance.Font = new System.Drawing.Font(e.Appearance.Font, e.Appearance.Font.Style | FontStyle.Italic);
                    }


                }



            }

            if (e.Column.FieldName == "AirVolume" || e.Column.FieldName == "WindResis")
            {
                if (_Fan.FanSelectionStateInfo != null && _Fan.FanSelectionStateInfo.fanSelectionState == FanSelectionState.LowNotFound)
                {
                    e.Appearance.ForeColor = Color.Red;
                    e.Appearance.Font = new System.Drawing.Font(e.Appearance.Font, e.Appearance.Font.Style | FontStyle.Italic);
                }
            }


        }

        private void TreeList_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            var _TreeList = sender as TreeList;
            if (_TreeList == null) { return; }
            var _Fan = _TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }
            var _FocusedColumn = _TreeList.FocusedColumn;
            if (FuncStr.NullToStr(e.Value) == string.Empty) { return; }
            List<int> _ListVentNum = new List<int>();
            List<FanDataModel> _List = new List<FanDataModel>();
            string _ErrorStr = string.Empty;
            if (_FocusedColumn.FieldName == "InstallSpace")
            {
                _List = m_ListFan.FindAll(p => p.InstallSpace == FuncStr.NullToStr(e.Value) && p.InstallFloor == _Fan.InstallFloor && p.ID != _Fan.ID && p.Scenario == _Fan.Scenario && !p.IsErased);
            }

            if (_FocusedColumn.FieldName == "InstallFloor")
            {
                _List = m_ListFan.FindAll(p => p.InstallSpace == _Fan.InstallSpace && p.InstallFloor == FuncStr.NullToStr(e.Value) && p.ID != _Fan.ID && p.Scenario == _Fan.Scenario && !p.IsErased);
            }
            if (_FocusedColumn.FieldName == "VentNum")
            {
                _List = m_ListFan.FindAll(p => p.InstallSpace == _Fan.InstallSpace && p.InstallFloor == _Fan.InstallFloor && p.ID != _Fan.ID && p.Scenario == _Fan.Scenario && !p.IsErased);

                var _Calculator = new VentSNCalculator(FuncStr.NullToStr(e.Value));
                if (_Calculator.SerialNumbers.Count > 0)
                {
                    _ListVentNum = _Calculator.SerialNumbers;
                }
            }

            if (_List != null && _List.Count > 0)
            {
                _List.ForEach(p =>
                {
                    if (p.ListVentQuan != null && p.ListVentQuan.Count > 0)
                    {
                        for (int i = 0; i < p.ListVentQuan.Count; i++)
                        {
                            if (_ListVentNum.Count > 0)
                            {
                                if (_ListVentNum.Contains(p.ListVentQuan[i]))
                                {
                                    if (_ErrorStr == string.Empty)
                                        _ErrorStr = p.FanNum;
                                    else
                                        _ErrorStr += "," + p.FanNum;
                                    break;
                                }
                            }
                            else
                            {
                                if (_Fan.ListVentQuan.Contains(p.ListVentQuan[i]))
                                {
                                    if (_ErrorStr == string.Empty)
                                        _ErrorStr = p.FanNum;
                                    else
                                        _ErrorStr += "," + p.FanNum;
                                    break;
                                }
                            }



                        }
                    }
                });

                if (_ErrorStr != string.Empty)
                {
                    e.Valid = false;
                    e.ErrorText = "当前风机与[" + _ErrorStr + "]冲突！";
                    return;
                }

            }
        }

        private void TreeList_InvalidValueException(object sender, DevExpress.XtraEditors.Controls.InvalidValueExceptionEventArgs e)
        {
            e.ExceptionMode = DevExpress.XtraEditors.Controls.ExceptionMode.NoAction;

            this.ToolTip.ShowBeak = true;

            this.ToolTip.ShowShadow = false;

            this.ToolTip.Rounded = false;

            var _Rect = TreeList.ActiveEditor.Bounds;

            var _P = _Rect.Location;

            _P.Offset(this.Location.X + _Rect.Width, this.Location.Y + _Rect.Height + 45);

            this.ToolTip.ShowHint(e.ErrorText, _P);
        }

        private void BtnOverView_Click(object sender, EventArgs e)
        {
            m_fmOverView.Init(m_ListFan, m_ListFanParameters, m_ListFanParametersSingle, m_ListFanParametersDouble, m_ListAxialFanParameters, m_ListAxialFanParametersDouble);

            m_fmOverView.Show();

            m_fmOverView.DataSourceChanged(m_ListFan);
        }

        private void TreeList_DataSourceChanged(object sender, EventArgs e)
        {
            m_fmOverView.DataSourceChanged(m_ListFan);
        }

        private void TreeList_HiddenEditor(object sender, EventArgs e)
        {
            m_fmOverView.DataSourceChanged(m_ListFan);
        }

        private void OnModelCopied(ThModelCopyMessage message)
        {
            foreach (var item in message.Data.ModelSystemMapping)
            {
                var _Guid = item.Key;
                var models = new List<FanDataModel>();
                var _Fan = m_ListFan.Find(p => p.ID == item.Value);
                if (_Fan != null)
                {
                    var _Json = FuncJson.Serialize(_Fan);
                    var _FanDataModel = FuncJson.Deserialize<FanDataModel>(_Json);

                    _FanDataModel.PID = "0";
                    _FanDataModel.ID = _Guid;
                    _FanDataModel.IsErased = false;
                    _FanDataModel.Name = _FanDataModel.Name;
                    _FanDataModel.InstallFloor = SetFanDataModelByFloor(_FanDataModel);
                    models.Add(_FanDataModel);

                    var _SonFan = m_ListFan.Find(p => p.PID == _Fan.ID);
                    if (_SonFan != null)
                    {
                        var _SonJson = FuncJson.Serialize(_SonFan);
                        var _SonFanData = FuncJson.Deserialize<FanDataModel>(_SonJson);

                        _SonFanData.ID = Guid.NewGuid().ToString();
                        _SonFanData.PID = _Guid;
                        _SonFanData.IsErased = false;
                        models.Add(_SonFanData);
                    }

                    // 更新数据源
                    if (models.Count > 0)
                    {
                        var _Index = m_ListFan.IndexOf(_Fan);
                        m_ListFan.InsertRange(_Index + 1, models);
                    }
                }
            }

            // 更新图纸
            var mappings = new Dictionary<FanDataModel, FanDataModel>();
            foreach (var item in message.Data.ModelSystemMapping)
            {
                var target = m_ListFan.Find(p => p.ID == item.Key);
                var source = m_ListFan.Find(p => p.ID == item.Value);
                if (target != null && source != null)
                {
                    mappings.Add(target, source);
                }
            }
            if (mappings.Count > 0)
            {
                ThFanSelectionService.Instance.ModelMapping = mappings;
                CommandHandlerBase.ExecuteFromCommandLine(false, "THFJSYSTEMCOPY");
            }

            // 更新界面
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();
            m_fmOverView.DataSourceChanged(m_ListFan);
        }

        private void OnModelDeleted(ThModelDeleteMessage message)
        {
            if (message.Data == null) { return; }

            foreach (var item in message.Data.ErasedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));

                //m_ListFan.RemoveAll(p => p.ID == FuncStr.NullToStr(message.Data.Model));

                //m_ListFan.RemoveAll(p => p.PID == FuncStr.NullToStr(message.Data.Model));

                if (_Fan == null) { return; }

                _Fan.IsErased = true;

                var _FanSon = m_ListFan.Find(p => p.PID == FuncStr.NullToStr(_ID));

                if (_FanSon != null) { _FanSon.IsErased = true; }
            }

            foreach (var item in message.Data.UnerasedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));

                //m_ListFan.RemoveAll(p => p.ID == FuncStr.NullToStr(message.Data.Model));

                //m_ListFan.RemoveAll(p => p.PID == FuncStr.NullToStr(message.Data.Model));

                if (_Fan == null) { return; }

                _Fan.IsErased = false;

                var _FanSon = m_ListFan.Find(p => p.PID == FuncStr.NullToStr(_ID));

                if (_FanSon != null) { _FanSon.IsErased = false; }
            }

            // 更新图纸
            var erasedModels = new List<FanDataModel>();
            foreach (var item in message.Data.ErasedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));
                if (_Fan != null)
                {
                    erasedModels.Add(_Fan);
                }

            }
            var unerasedModels = new List<FanDataModel>();
            foreach (var item in message.Data.UnerasedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));
                if (_Fan != null)
                {
                    unerasedModels.Add(_Fan);
                }
            }
            ThFanSelectionService.Instance.ErasedModels = erasedModels;
            ThFanSelectionService.Instance.UnerasedModels = unerasedModels;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THFJSYSTEMERASE");

            // 更新界面
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();
        }

        private void OnModelUndo(ThModelUndoMessage message)
        {
            foreach (var item in message.Data.UnappendedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));

                if (_Fan == null) { continue; }

                _Fan.IsErased = true;

                var _FanSon = m_ListFan.Find(p => p.PID == FuncStr.NullToStr(_ID));

                if (_FanSon != null) { _FanSon.IsErased = true; }
            }

            foreach (var item in message.Data.ReappendedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));

                if (_Fan == null) { continue; }

                _Fan.IsErased = false;

                var _FanSon = m_ListFan.Find(p => p.PID == FuncStr.NullToStr(_ID));

                if (_FanSon != null) { _FanSon.IsErased = false; }
            }

            // 更新图纸
            var erasedModels = new List<FanDataModel>();
            foreach (var item in message.Data.UnappendedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));
                if (_Fan != null)
                {
                    erasedModels.Add(_Fan);
                }

            }
            var unerasedModels = new List<FanDataModel>();
            foreach (var item in message.Data.ReappendedModels)
            {
                var _ID = item.Key;
                var _Fan = m_ListFan.Find(p => p.ID == FuncStr.NullToStr(_ID));
                if (_Fan != null)
                {
                    unerasedModels.Add(_Fan);
                }
            }
            ThFanSelectionService.Instance.ErasedModels = erasedModels;
            ThFanSelectionService.Instance.UnerasedModels = unerasedModels;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THFJSYSTEMERASE");

            // 更新图纸
            TreeList.RefreshDataSource();
            this.TreeList.ExpandAll();
        }

        private void OnModelBeginSave(ThModelBeginSaveMessage message)
        {
            if (m_ListFan == null || m_ListFan.Count == 0 || FuncStr.NullToStr(message.Data.FileName) == string.Empty) { return; }
            TreeList.PostEditor();

            // 保存到图纸NOD中
            var dataSource = new ThFanModelDbDataSource()
            {
                Models = m_ListFan,
            };
            dataSource.Save(Active.Database);

            // 用当前图纸名更新标题
            string _Filename = Path.GetFileName(FuncStr.NullToStr(message.Data.FileName));
            this.Text = "风机选型 - " + Path.GetFileNameWithoutExtension(_Filename);
        }

        private void CheckSysAverage_CheckedChanged(object sender, EventArgs e)
        {
            CheckSysCheckedChanged();
        }

        private void CheckSysCheckedChanged()
        {
            TreeList.PostEditor();
            var _Fan = TreeList.GetFocusedRow() as FanDataModel;
            if (_Fan == null) { return; }
            if (_Fan.IsSysAverage)
            {

                if (_Fan.PID == "0")
                {

                    if (_Fan.IsManualInputAirVolume)
                    {
                        var _AirCalcValue = _Fan.SysAirVolume / _Fan.VentQuan;

                        MainSysAirVolumeCalc(_Fan, _AirCalcValue);

                        var _SonFan = m_ListFan.Find(p => p.PID == _Fan.ID);


                        if (_SonFan != null)
                        {

                            if (_SonFan.IsManualInputAirVolume)
                            {
                                var _SonAirCalcValue = _SonFan.SysAirVolume / _Fan.VentQuan;

                                SonSysAirVolumeCalc(_SonFan, _SonAirCalcValue);
                            }
                            else
                            {

                                var _SonAirCalcValue = _SonFan.AirCalcValue * _SonFan.AirCalcFactor / _Fan.VentQuan;

                                SonSysAirVolumeCalc(_SonFan, _SonAirCalcValue);
                            }


                        }

                    }
                    else
                    {
                        var _AirCalcValue = _Fan.AirCalcValue * _Fan.AirCalcFactor / _Fan.VentQuan;

                        MainSysAirVolumeCalc(_Fan, _AirCalcValue);

                        var _SonFan = m_ListFan.Find(p => p.PID == _Fan.ID);


                        if (_SonFan != null)
                        {

                            if (_SonFan.IsManualInputAirVolume)
                            {
                                var _SonAirCalcValue = _SonFan.SysAirVolume / _Fan.VentQuan;

                                SonSysAirVolumeCalc(_SonFan, _SonAirCalcValue);
                            }
                            else
                            {

                                var _SonAirCalcValue = _SonFan.AirCalcValue * _SonFan.AirCalcFactor / _Fan.VentQuan;

                                SonSysAirVolumeCalc(_SonFan, _SonAirCalcValue);
                            }


                        }


                    }


                }
                else
                {
                    var _MainFan = m_ListFan.Find(p => p.ID == _Fan.PID);
                    if (_MainFan != null)
                    {
                        if (_Fan.IsManualInputAirVolume)
                        {
                            var _AirCalcValue = _Fan.SysAirVolume / _MainFan.VentQuan;

                            SonSysAirVolumeCalc(_Fan, _AirCalcValue);
                        }
                        else
                        {
                            var _AirCalcValue = _Fan.AirCalcValue * _Fan.AirCalcFactor / _MainFan.VentQuan;

                            SonSysAirVolumeCalc(_Fan, _AirCalcValue);
                        }

                        //var _AirCalcValue = _Fan.AirCalcValue * _Fan.AirCalcFactor / _MainFan.VentQuan;

                        //var _Rem = FuncStr.NullToInt(_AirCalcValue) % 50;

                        //if (_Rem != 0)
                        //{
                        //    var _UnitsDigit = FindNum(FuncStr.NullToInt(_AirCalcValue), 1);

                        //    var _TensDigit = FindNum(FuncStr.NullToInt(_AirCalcValue), 2);

                        //    var _Tmp = FuncStr.NullToInt(_TensDigit.ToString() + _UnitsDigit.ToString());

                        //    if (_Tmp < 50)
                        //    {
                        //        var _DifferenceValue = 50 - _Tmp;
                        //        _Fan.AirVolume = FuncStr.NullToInt(_AirCalcValue) + _DifferenceValue;

                        //    }

                        //    else
                        //    {
                        //        var _DifferenceValue = 100 - _Tmp;
                        //        _Fan.AirVolume = FuncStr.NullToInt(_AirCalcValue) + _DifferenceValue;
                        //    }
                        //}
                        //else
                        //{
                        //    _Fan.AirVolume = FuncStr.NullToInt(_AirCalcValue);
                        //}
                    }



                }



            }
            else
            {
                _Fan.AirVolume = _Fan.SysAirVolume;
            }
            TreeList.Refresh();
        }

        private void SonSysAirVolumeCalc(FanDataModel _SonFan, double _SonAirCalcValue)
        {
            var _SonRem = FuncStr.NullToInt(_SonAirCalcValue) % 50;

            if (_SonRem != 0)
            {
                var _SonUnitsDigit = FindNum(FuncStr.NullToInt(_SonAirCalcValue), 1);

                var _SonTensDigit = FindNum(FuncStr.NullToInt(_SonAirCalcValue), 2);

                var _SonTmp = FuncStr.NullToInt(_SonTensDigit.ToString() + _SonUnitsDigit.ToString());

                if (_SonTmp < 50)
                {
                    var _DifferenceValue = 50 - _SonTmp;
                    _SonFan.AirVolume = FuncStr.NullToInt(_SonAirCalcValue) + _DifferenceValue;

                }

                else
                {
                    var _DifferenceValue = 100 - _SonTmp;
                    _SonFan.AirVolume = FuncStr.NullToInt(_SonAirCalcValue) + _DifferenceValue;
                }
            }
            else
            {
                _SonFan.AirVolume = FuncStr.NullToInt(_SonAirCalcValue);
            }
        }

        private void MainSysAirVolumeCalc(FanDataModel _Fan, double _AirCalcValue)
        {
            var _Rem = FuncStr.NullToInt(_AirCalcValue) % 50;

            if (_Rem != 0)
            {
                var _UnitsDigit = FindNum(FuncStr.NullToInt(_AirCalcValue), 1);

                var _TensDigit = FindNum(FuncStr.NullToInt(_AirCalcValue), 2);

                var _Tmp = FuncStr.NullToInt(_TensDigit.ToString() + _UnitsDigit.ToString());

                if (_Tmp < 50)
                {
                    var _DifferenceValue = 50 - _Tmp;
                    _Fan.AirVolume = FuncStr.NullToInt(_AirCalcValue) + _DifferenceValue;

                }

                else
                {
                    var _DifferenceValue = 100 - _Tmp;
                    _Fan.AirVolume = FuncStr.NullToInt(_AirCalcValue) + _DifferenceValue;
                }
            }
            else
            {
                _Fan.AirVolume = FuncStr.NullToInt(_AirCalcValue);
            }
        }

        private void TxtVentNum_EditValueChanged(object sender, EventArgs e)
        {
            CheckSysCheckedChanged();
            SetFanModel();
            TreeList.Refresh();
        }
    }
}
