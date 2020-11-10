using System;

namespace TianHua.FanSelection.UI
{
    public partial class CtlCloister : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnMinVolumChanged { get; set; }

        public CtlCloister()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnMinVolumChanged = action;
            this.RadSpray.SelectedIndex = Model.ExhaustModel.IsSpray ? 0 : 1;
            this.TxtHeight.Text = Model.ExhaustModel.SpaceHeight;
            Model.ExhaustModel.SpatialTypes = "办公室、学校、客厅、走道";
            Model.ExhaustModel.MinAirVolume = this.TxtMinUnitVolume.Text;
        }

        private void TxtHeightChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SpaceHeight = TxtHeight.Text;
            OnMinVolumChanged();
        }

        private void RadSpraySelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.IsSpray = this.RadSpray.SelectedIndex == 0 ? true : false;
            OnMinVolumChanged();
        }

    }
}
