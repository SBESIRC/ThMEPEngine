using System;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class CtlGarage : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnMinVolumChanged { get; set; }

        public CtlGarage()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnMinVolumChanged = action;
            this.RadSpray.SelectedIndex = Model.ExhaustModel.IsSpray ? 0 : 1;
            Model.ExhaustModel.SpatialTypes = "汽车库";
            this.TxtHeight.Text = Model.ExhaustModel.SpaceHeight;
            this.TxtMinUnitVolume.Text = Model.ExhaustModel.MinAirVolume;
        }

        private void TxtHeightChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SpaceHeight = TxtHeight.Text;
            this.TxtMinUnitVolume.Text = ExhaustModelCalculator.GetMinVolumeForGarage(Model.ExhaustModel).ToString();
            Model.ExhaustModel.MinAirVolume = this.TxtMinUnitVolume.Text;
            OnMinVolumChanged();
        }

        private void RadSpraySelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.IsSpray = this.RadSpray.SelectedIndex == 0 ? true : false;
            OnMinVolumChanged();
        }

    }
}
