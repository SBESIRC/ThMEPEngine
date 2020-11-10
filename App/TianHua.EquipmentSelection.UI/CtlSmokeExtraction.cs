using System;

namespace TianHua.FanSelection.UI
{
    public partial class CtlSmokeExtraction : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private Action OnMinVolumChanged { get; set; }

        public CtlSmokeExtraction()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel,Action action)
        {
            Model = _FanDataModel;
            OnMinVolumChanged = action;
            this.simpleLabelItem1.Text = Model.ExhaustModel.ExhaustCalcType;
            if (Model.ExhaustModel.ExhaustCalcType == "中庭-周围场所设有排烟系统")
            {
                this.TxtMinUnitVolume.Text = "107000";
                this.Notetext.Text = ThFanSelectionUICommon.NOTE_CENTER_EXTRACTION;
            }
            else
            {
                this.TxtMinUnitVolume.Text = "40000";
                this.Notetext.Text = ThFanSelectionUICommon.NOTE_CENTER_EXTRACTION_NOSMOKE;
            }
            Model.ExhaustModel.MinAirVolume = this.TxtMinUnitVolume.Text;
            this.RadSpray.SelectedIndex = Model.ExhaustModel.IsSpray ? 0 : 1;
            this.ComBoxSpatialType.Text = Model.ExhaustModel.SpatialTypes;
            this.TxtHeight.Text = Model.ExhaustModel.SpaceHeight;
        }

        private void SpatialTypeSelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SpatialTypes = ComBoxSpatialType.Text;
            OnMinVolumChanged();
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
