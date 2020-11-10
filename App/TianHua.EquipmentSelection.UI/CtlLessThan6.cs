using System;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class CtlLessThan6 : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnMinVolumChanged { get; set; }

        public CtlLessThan6()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnMinVolumChanged = action;
            this.RadSpray.SelectedIndex = Model.ExhaustModel.IsSpray ? 0 : 1;
            this.ComBoxSpatialType.Text = Model.ExhaustModel.SpatialTypes;
            this.TxtHeight.Text = Model.ExhaustModel.SpaceHeight;
            this.TxtArea.Text = Model.ExhaustModel.CoveredArea;
            Model.ExhaustModel.UnitVolume = this.TxtUnitVolume.Text;
            this.TxtMinUnitVolume.Text = Model.ExhaustModel.MinAirVolume;
        }

        private void SpatialTypeChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SpatialTypes = ComBoxSpatialType.Text;
            OnMinVolumChanged();
        }

        private void TxtHeightChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SpaceHeight = TxtHeight.Text;
            OnMinVolumChanged();
        }

        private void TxtAreaChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.CoveredArea = TxtArea.Text;
            UpdateMinAirVolume();
        }

        private void UpdateMinAirVolume()
        {
            TxtMinUnitVolume.Text = ExhaustModelCalculator.GetMinVolumeForLess6(Model.ExhaustModel).ToString();
            Model.ExhaustModel.MinAirVolume = TxtMinUnitVolume.Text;
        }

        private void RadSpraySelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.IsSpray = this.RadSpray.SelectedIndex == 0 ? true : false;
            OnMinVolumChanged();
        }

    }
}
