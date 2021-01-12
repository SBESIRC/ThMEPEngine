using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Model;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class fmAirVolumeCalc : DevExpress.XtraEditors.XtraForm
    {
        public List<FanDataModel> m_ListFan { get; set; }

        public FanDataModel m_Fan { get; set; }

        //public string CurrentScenairo { get; set; }

        public fmAirVolumeCalc()
        {
            InitializeComponent();
        }

 
        public void InitForm(FanDataModel _FanDataModel)
        {
            var _Json = FuncJson.Serialize(_FanDataModel);
            m_Fan = FuncJson.Deserialize<FanDataModel>(_Json);

            //m_Fan = _FanDataModel;
            if (m_ListFan == null) m_ListFan = new List<FanDataModel>();
            m_ListFan.Add(m_Fan);

            Gdc.DataSource = m_ListFan;
            Gdv.RefreshData();
            if (m_Fan.Scenario == "消防加压送风")
            {
                this.TxtAirCalcValue.ContextImageOptions.SvgImage = Properties.Resources.计算器;
                this.TxtAirCalcValue.ReadOnly = true;
            }
            else
            {
                this.TxtAirCalcValue.ReadOnly = false;
                this.TxtAirCalcValue.ContextImageOptions.SvgImage = null;
            }

            CheckIsManualInput.Checked = m_Fan.IsManualInputAirVolume;
            if (CheckIsManualInput.Checked)
            {
                TxtManualInput.Text = FuncStr.NullToStr(m_Fan.SysAirVolume);
            }
        }

        private void Gdv_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            var _Fan = Gdv.GetFocusedRow() as FanDataModel;

            if (_Fan == null) { return; }

            if (e.Column.FieldName == "AirCalcValue")
            {

            }

            if (e.Column.FieldName == "AirCalcFactor")
            {
                if (_Fan.IsFireModel())
                {
                    if (_Fan.AirCalcFactor < 1.2)
                    {
                        _Fan.AirCalcFactor = 1.2;
                    }
                }
                else
                {
                    if (_Fan.AirCalcFactor < 1)
                    {
                        _Fan.AirCalcFactor = 1;
                    }
                }
            }

            SysAirCalc(_Fan);
        }

        private void SysAirCalc(FanDataModel _Fan)
        {
            var _Value = _Fan.AirCalcValue * _Fan.AirCalcFactor;

            var _Rem = FuncStr.NullToInt(_Value) % 50;

            if (_Rem != 0)
            {
                var _UnitsDigit = FindNum(FuncStr.NullToInt(_Value), 1);

                var _TensDigit = FindNum(FuncStr.NullToInt(_Value), 2);

                var _Tmp = FuncStr.NullToInt(_TensDigit.ToString() + _UnitsDigit.ToString());

                if (_Tmp < 50)
                {
                    var _DifferenceValue = 50 - _Tmp;
                    _Fan.SysAirVolume = FuncStr.NullToInt(_Value) + _DifferenceValue;
                }
                else
                {
                    var _DifferenceValue = 100 - _Tmp;
                    _Fan.SysAirVolume = FuncStr.NullToInt(_Value) + _DifferenceValue;
                }
            }
            else
            {
                _Fan.SysAirVolume = FuncStr.NullToInt(_Value);
            }
        }

        private void UpdateVolume()
        {
            var _Fan = Gdv.GetFocusedRow() as FanDataModel;
            if (_Fan == null)
            { return; }

            if (_Fan.IsFireModel())
            {
                if (_Fan.AirCalcFactor < 1.2)
                {
                    _Fan.AirCalcFactor = 1.2;
                }
            }
            else
            {
                if (_Fan.AirCalcFactor < 1.1)
                {
                    _Fan.AirCalcFactor = 1.1;
                }
            }
            var _Value = _Fan.AirCalcValue * _Fan.AirCalcFactor;
            var _Rem = FuncStr.NullToInt(_Value) % 50;
            if (_Rem != 0)
            {
                var _UnitsDigit = FindNum(FuncStr.NullToInt(_Value), 1);
                var _TensDigit = FindNum(FuncStr.NullToInt(_Value), 2);
                var _Tmp = FuncStr.NullToInt(_TensDigit.ToString() + _UnitsDigit.ToString());
                if (_Tmp < 50)
                {
                    var _DifferenceValue = 50 - _Tmp;
                    _Fan.SysAirVolume = FuncStr.NullToInt(_Value) + _DifferenceValue;
                }
                else
                {
                    var _DifferenceValue = 100 - _Tmp;
                    _Fan.SysAirVolume = FuncStr.NullToInt(_Value) + _DifferenceValue;
                }
            }
            else
            {
                _Fan.SysAirVolume = FuncStr.NullToInt(_Value);

            }
        }

        public int FindNum(int _Num, int _N)
        {
            int _Power = (int)Math.Pow(10, _N);
            return (_Num - _Num / _Power * _Power) * 10 / _Power;
        }

        private void TxtAirCalcValue_Click(object sender, EventArgs e)
        {
            if (m_Fan.Scenario != "消防加压送风") { return; }

            using (var _fmFanVolumeCalc = new fmFanVolumeCalc(m_Fan))
            {
                if (_fmFanVolumeCalc.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                //CurrentScenairo = _fmFanVolumeCalc.CurrentScenairo;
                switch (_fmFanVolumeCalc.Model.FireScenario)
                {
                    case "消防电梯前室":
                        FireFrontModel firemodel = _fmFanVolumeCalc.Model as FireFrontModel;
                        m_Fan.FanVolumeModel = firemodel;
                        m_Fan.AirCalcValue = Convert.ToInt32(Math.Round(Math.Max(firemodel.QueryValue, firemodel.TotalVolume)));
                        break;
                    case "独立或合用前室（楼梯间自然）":
                        FontroomNaturalModel fontroommodel = _fmFanVolumeCalc.Model as FontroomNaturalModel;
                        m_Fan.FanVolumeModel = _fmFanVolumeCalc.Model as FontroomNaturalModel;
                        m_Fan.AirCalcValue = Convert.ToInt32(Math.Round(Math.Max(fontroommodel.QueryValue, fontroommodel.TotalVolume)));
                        break;
                    case "独立或合用前室（楼梯间送风）":
                        FontroomWindModel fontroomwindmodel = _fmFanVolumeCalc.Model as FontroomWindModel;
                        m_Fan.FanVolumeModel = _fmFanVolumeCalc.Model as FontroomWindModel;
                        m_Fan.AirCalcValue = Convert.ToInt32(Math.Round(Math.Max(fontroomwindmodel.QueryValue, fontroomwindmodel.TotalVolume)));
                        break;
                    case "楼梯间（前室不送风）":
                        StaircaseNoAirModel staircasenoairmodel = _fmFanVolumeCalc.Model as StaircaseNoAirModel;
                        m_Fan.FanVolumeModel = _fmFanVolumeCalc.Model as StaircaseNoAirModel;
                        m_Fan.AirCalcValue = Convert.ToInt32(Math.Round(Math.Max(staircasenoairmodel.QueryValue, staircasenoairmodel.TotalVolume)));
                        break;
                    case "楼梯间（前室送风）":
                        StaircaseAirModel staircaseairmodel = _fmFanVolumeCalc.Model as StaircaseAirModel;
                        m_Fan.FanVolumeModel = _fmFanVolumeCalc.Model as StaircaseAirModel;
                        m_Fan.AirCalcValue = Convert.ToInt32(Math.Round(Math.Max(staircaseairmodel.QueryValue, staircaseairmodel.TotalVolume)));
                        break;
                    case "封闭避难层（间）、避难走道":
                        RefugeRoomAndCorridorModel refugeroomandcorridormodel = _fmFanVolumeCalc.Model as RefugeRoomAndCorridorModel;
                        m_Fan.FanVolumeModel = _fmFanVolumeCalc.Model as RefugeRoomAndCorridorModel;
                        m_Fan.AirCalcValue = Convert.ToInt32(Math.Round(Math.Max(refugeroomandcorridormodel.QueryValue, refugeroomandcorridormodel.WindVolume)));
                        break;
                    case "避难走道前室":
                        RefugeFontRoomModel refugefontroommodel = _fmFanVolumeCalc.Model as RefugeFontRoomModel;
                        m_Fan.FanVolumeModel = _fmFanVolumeCalc.Model as RefugeFontRoomModel;
                        m_Fan.AirCalcValue = Convert.ToInt32(Math.Round(Math.Max(refugefontroommodel.QueryValue, refugefontroommodel.DoorOpeningVolume)));
                        break;
                    default:
                        break;
                }
                Gdv.FocusedColumn = ColFactor;
                Gdv.FocusedColumn = ColCalcValue;
                UpdateVolume();
                Gdv.PostEditor();
                Gdv.RefreshData();

            }
        }

        private void CheckIsManualInput_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckIsManualInput.Checked)
            {
                TxtManualInput.Enabled = true;
                Gdc.Enabled = false;

            }
            else
            {

                TxtManualInput.Enabled = false;
                Gdc.Enabled = true;
                SysAirCalc(m_Fan);
                Gdv.RefreshData();
            }
        }

        private void TxtManualInput_EditValueChanged(object sender, EventArgs e)
        {
            var _ManualInput = FuncStr.NullToInt(TxtManualInput.Text);

            if (_ManualInput == 0) { return; }


            var _Rem = FuncStr.NullToInt(_ManualInput) % 50;
            if (_Rem != 0)
            {
                var _UnitsDigit = FindNum(FuncStr.NullToInt(_ManualInput), 1);
                var _TensDigit = FindNum(FuncStr.NullToInt(_ManualInput), 2);
                var _Tmp = FuncStr.NullToInt(_TensDigit.ToString() + _UnitsDigit.ToString());
                if (_Tmp < 50)
                {
                    var _DifferenceValue = 50 - _Tmp;
                    TxtManualInput.Text = FuncStr.NullToStr(FuncStr.NullToInt(_ManualInput) + _DifferenceValue);

                }
                else
                {
                    var _DifferenceValue = 100 - _Tmp;
                    TxtManualInput.Text = FuncStr.NullToStr(FuncStr.NullToInt(_ManualInput) + _DifferenceValue);
                }
            }
            else
            {
                TxtManualInput.Text = FuncStr.NullToStr(FuncStr.NullToInt(_ManualInput));
            }
        }
    }
}
