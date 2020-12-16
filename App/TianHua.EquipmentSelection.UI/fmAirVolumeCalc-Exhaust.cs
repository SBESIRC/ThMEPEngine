using System;
using System.Windows.Forms;
using TianHua.FanSelection.Function;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI
{
    public partial class fmAirVolumeCalc_Exhaust : DevExpress.XtraEditors.XtraForm
    {
        public FanDataModel Model { get; set; }

        public fmAirVolumeCalc_Exhaust()
        {
            InitializeComponent();
        }

        public void InitForm(FanDataModel _FanDataModel)
        {
            var _Json = FuncJson.Serialize(_FanDataModel);

            Model = FuncJson.Deserialize<FanDataModel>(_Json);

            if (Model.ExhaustModel.IsNull())
            {
                TxtCalcValue.Text = "无";
                TxtEstimatedValue.Text = FuncStr.NullToStr(Model.AirCalcValue);
                //Model.AirCalcFactor = 1.2;
                TxtFactor.Text = FuncStr.NullToStr(Model.AirCalcFactor);
                return;
            }

            TxtCalcValue.Text = ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel);
            TxtEstimatedValue.Text = FuncStr.NullToStr(Model.ExhaustModel.EstimateAirVolum.NullToDouble());
            int maxairvalue = Math.Max(TxtCalcValue.Text.NullToInt(), TxtEstimatedValue.Text.NullToInt());
            Model.SysAirVolume = SysAirCalc(Model.AirCalcFactor, maxairvalue);
            TxtFactor.Text = FuncStr.NullToStr(Model.AirCalcFactor);
            TxtAirVolume.Text = FuncStr.NullToStr(Model.SysAirVolume);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {

        }

        private void TxtCalcValue_Click(object sender, EventArgs e)
        {
            fmScenario _fmScenario = new fmScenario();
            _fmScenario.InitForm(Model);
            if (_fmScenario.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            Model.ExhaustModel = _fmScenario.Model.ExhaustModel;
            if (!Model.ExhaustModel.IsNull())
            {
                this.TxtCalcValue.Text = ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel);
                Model.ExhaustModel.EstimateAirVolum = TxtEstimatedValue.Text;
            }
        }

        private void TxtCalcValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;

        }

        private void TxtAirVolume_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;


        }

        private void fmAirVolumeCalc_Exhaust_Load(object sender, EventArgs e)
        {
       
        }

        //private void CalculateAirVolumeChanged(object sender, EventArgs e)
        //{
        //    Model.AirVolume = this.TxtAirVolume.Text.NullToInt();
        //}

        private void EstimateValueChanged(object sender, EventArgs e)
        {
            int maxairvalue = Math.Max(TxtCalcValue.Text.NullToInt(), TxtEstimatedValue.Text.NullToInt());
            Model.SysAirVolume = SysAirCalc(Model.AirCalcFactor, maxairvalue);
            if (Model.ExhaustModel != null)
            {
                Model.ExhaustModel.EstimateAirVolum = this.TxtEstimatedValue.Text;
                Model.AirCalcValue = Math.Max(ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel).NullToInt(), Model.ExhaustModel.EstimateAirVolum.NullToInt());
            }
            else
            {
                Model.AirCalcValue = FuncStr.NullToInt(TxtEstimatedValue.Text);
            }
            UpdateAirVolume();
        }

        private void SelectFactorChanged(object sender, EventArgs e)
        {
            Model.AirCalcFactor = this.TxtFactor.Text.NullToDouble();
            UpdateAirVolume();
        }

        private void CalculateValueChanged(object sender, EventArgs e)
        {
            int maxairvalue = 0;
            if (!Model.ExhaustModel.IsNull())
            {
                maxairvalue = Math.Max(ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel).NullToInt(), Model.ExhaustModel.EstimateAirVolum.NullToInt());
                Model.AirCalcValue = maxairvalue;
            }
            UpdateAirVolume();
        }

        private void UpdateAirVolume()
        {
            int maxairvalue = Math.Max(TxtCalcValue.Text.NullToInt(), TxtEstimatedValue.Text.NullToInt());
            Model.SysAirVolume = SysAirCalc(Model.AirCalcFactor, maxairvalue);
            this.TxtAirVolume.Text = Model.SysAirVolume.NullToStr();
        }

        private void FactorChangedCheck(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                this.TxtFactor.Text = ExhaustModelCalculator.SelectionFactorCheck(this.TxtFactor.Text);
            }
        }

        private void FactorLeaveCheck(object sender, EventArgs e)
        {
            this.TxtFactor.Text = ExhaustModelCalculator.SelectionFactorCheck(this.TxtFactor.Text);
        }

        private int SysAirCalc(double calcfactor, double airvalue)
        {
            var _Value = airvalue * calcfactor;
            int sysairvolume = 0;
            var _Rem = FuncStr.NullToInt(_Value) % 100;
            if (_Rem == 0)
            {
                sysairvolume = FuncStr.NullToInt(_Value);
            }
            else
            {
                if (_Rem < 50)
                {
                    sysairvolume = FuncStr.NullToInt(_Value - _Rem + 50);
                }
                else
                {
                    sysairvolume = FuncStr.NullToInt(_Value - _Rem + 100);
                }
            }
            return FuncStr.NullToInt(sysairvolume);
        }

    }
}
