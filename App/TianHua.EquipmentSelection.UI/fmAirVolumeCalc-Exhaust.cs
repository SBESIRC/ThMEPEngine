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
                Model.AirCalcFactor = 1.2;
                TxtFactor.Text = FuncStr.NullToStr(Model.AirCalcFactor);
                return;
            }

            this.TxtCalcValue.Text = ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel);
            TxtEstimatedValue.Text = FuncStr.NullToStr(Model.AirCalcValue);
            TxtFactor.Text = FuncStr.NullToStr(Model.AirCalcFactor);
            TxtAirVolume.Text = FuncStr.NullToStr(Model.AirVolume);
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
            Model.AirCalcValue = this.TxtEstimatedValue.Text.NullToInt();
            UpdateAirVolume();
        }

        private void SelectFactorChanged(object sender, EventArgs e)
        {
            Model.AirCalcFactor = this.TxtFactor.Text.NullToDouble();
            UpdateAirVolume();
        }

        private void CalculateValueChanged(object sender, EventArgs e)
        {
            UpdateAirVolume();
        }

        private void UpdateAirVolume()
        {
            int maxcalvalue = FuncStr.NullToInt(Math.Round(Math.Max(this.TxtCalcValue.Text.NullToDouble(), this.TxtEstimatedValue.Text.NullToDouble()) * this.TxtFactor.Text.NullToDouble()));
            Model.AirVolume = maxcalvalue==0 ? 0 : ExhaustModelCalculator.RoundUpToFifty(maxcalvalue);
            this.TxtAirVolume.Text = Model.AirVolume.NullToStr();
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
    }
}
