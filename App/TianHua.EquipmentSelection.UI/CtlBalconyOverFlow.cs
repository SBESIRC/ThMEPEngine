using System;
using TianHua.FanSelection.Model;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class CtlBalconyOverFlow : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnVolumChanged { get; set; }

        public CtlBalconyOverFlow()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnVolumChanged = action;
            this.TxtFuelToBalcony.Text = Model.ExhaustModel.Spill_FuelBalcony;
            this.TxtBalconySmokeBottom.Text = Model.ExhaustModel.Spill_BalconySmokeBottom;
            this.TxtFireOpening.Text = Model.ExhaustModel.Spill_FireOpening;
            this.TxtOpenBalcony.Text = Model.ExhaustModel.Spill_OpenBalcony;
        }

        private void FuelToBalconyChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Spill_FuelBalcony = this.TxtFuelToBalcony.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        private void BalconySmokeBottomChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Spill_BalconySmokeBottom = this.TxtBalconySmokeBottom.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        private void FireOpeningChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Spill_FireOpening = this.TxtFireOpening.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        private void OpenBalconyChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Spill_OpenBalcony = this.TxtOpenBalcony.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        public override void UpdateCalcAirVolum(ExhaustCalcModel model)
        {
            if (Model == null)
            {
                return;
            }
            this.TxtCalculateVolume.Text = ExhaustModelCalculator.GetCalcAirVolum(model);
            Model.ExhaustModel.Spill_CalcAirVolum = this.TxtCalculateVolume.Text;
            Model.ExhaustModel.Final_CalcAirVolum = Model.ExhaustModel.Spill_CalcAirVolum;
            OnVolumChanged();
        }

    }
}
