using System;
using TianHua.FanSelection.Model;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class CtlAxisymmetric : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnVolumChanged { get; set; }

        public CtlAxisymmetric()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnVolumChanged = action;
            this.textEdit1.Text = Model.ExhaustModel.Axial_HighestHeight;
            this.textEdit2.Text = Model.ExhaustModel.Axial_HangingWallGround;
            this.textEdit3.Text = Model.ExhaustModel.Axial_FuelFloor;
            this.textEdit4.Text = Model.ExhaustModel.Axial_CalcAirVolum;
            Model.ExhaustModel.Final_CalcAirVolum = Model.ExhaustModel.Axial_CalcAirVolum;
        }

        private void textEdit1ValueChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Axial_HighestHeight = this.textEdit1.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        private void textEdit2ValueChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Axial_HangingWallGround = this.textEdit2.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        private void textEdit3ValueChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.Axial_FuelFloor = this.textEdit3.Text;
            UpdateCalcAirVolum(Model.ExhaustModel);
        }

        public override void UpdateCalcAirVolum(ExhaustCalcModel model)
        {
            if (Model == null)
            {
                return;
            }
            this.textEdit4.Text = ExhaustModelCalculator.GetCalcAirVolum(model);
            Model.ExhaustModel.Axial_CalcAirVolum = this.textEdit4.Text;
            Model.ExhaustModel.Final_CalcAirVolum = Model.ExhaustModel.Axial_CalcAirVolum;

            if (model.SpaceHeight.NullToDouble() < 3)
            {
                this.textEdit1.ReadOnly = true;
            }
            else
            {
                this.textEdit1.ReadOnly = false;
            }
            OnVolumChanged();
        }
    }
}
