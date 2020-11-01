using System;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class CtlCloistersAndRooms : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnMinVolumChanged { get; set; }

        public CtlCloistersAndRooms()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnMinVolumChanged = action;
            this.RadSpray.SelectedIndex = Model.ExhaustModel.IsSpray ? 0 : 1;
            this.TxtHeight.Text = Model.ExhaustModel.SpaceHeight;
            this.TxtArea.Text = Model.ExhaustModel.CoveredArea;
            this.TxtMinUnitVolume.Text = Model.ExhaustModel.MinAirVolume;
            Model.ExhaustModel.UnitVolume = this.TxtUnitVolume.Text;
            Model.ExhaustModel.SpatialTypes = "办公室、学校、客厅、走道";
            Model.ExhaustModel.MinAirVolume = this.TxtMinUnitVolume.Text;
        }

        private void RadSpraySelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.IsSpray = this.RadSpray.SelectedIndex == 0 ? true : false;
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
            TxtMinUnitVolume.Text = ExhaustModelCalculator.GetMinVolumeForCtlCloistersRooms(Model.ExhaustModel).ToString();
            Model.ExhaustModel.MinAirVolume = TxtMinUnitVolume.Text;
        }

    }
}
