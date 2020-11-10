using System;
using System.IO;
using ThCADExtension;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI
{
    public partial class CtlGreaterThan6 : CtlExhaustControlBase
    {
        private FanDataModel Model { get; set; }
        private ExhaustModelLoader Loader { get; set; }
        private Action OnChanged { get; set; }

        public CtlGreaterThan6()
        {
            InitializeComponent();
        }

        public override void InitForm(FanDataModel _FanDataModel, Action action)
        {
            Model = _FanDataModel;
            Loader = new ExhaustModelLoader();
            Loader.LoadFromFile(Path.Combine(ThCADCommon.SupportPath(), "最小排烟量.json"));
            OnChanged = action;
            this.RadSpray.SelectedIndex = Model.ExhaustModel.IsSpray ? 0 : 1;
            this.ComBoxSpatialType.Text = Model.ExhaustModel.SpatialTypes;
            this.TxtHeight.Text = Model.ExhaustModel.SpaceHeight;
            this.TxtMinUnitVolume.Text = Model.ExhaustModel.MinAirVolume;
        }

        private void SpatialTypeSelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SpatialTypes = ComBoxSpatialType.Text;
            TxtMinUnitVolume.Text = ExhaustModelCalculator.GetMinVolumeForGreater6(Loader, Model.ExhaustModel).ToString();
            OnChanged();
        }

        private void TxtHeightChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.SpaceHeight = TxtHeight.Text;
            this.TxtMinUnitVolume.Text = ExhaustModelCalculator.GetMinVolumeForGreater6(Loader, Model.ExhaustModel).ToString();
            Model.ExhaustModel.MinAirVolume = this.TxtMinUnitVolume.Text;
            OnChanged();
        }

        private void RadSpraySelectedChanged(object sender, EventArgs e)
        {
            Model.ExhaustModel.IsSpray = this.RadSpray.SelectedIndex == 0 ? true : false;
            this.TxtMinUnitVolume.Text = ExhaustModelCalculator.GetMinVolumeForGreater6(Loader, Model.ExhaustModel).ToString();
            Model.ExhaustModel.MinAirVolume = this.TxtMinUnitVolume.Text;
            OnChanged();
        }
    }
}
